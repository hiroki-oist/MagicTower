//  MagicTowerStore.swift
//  課金（Apple In-App Purchase）の実体。StoreKit 2。
//
//  Unity 側（GoldShop / AppleStore.cs）から C の関数として呼ぶ。
//  **呼び返しは使わず**、Unity から毎コマ MTStorePoll() を読む形にしてある
//  （UnitySendMessage を使うと橋渡しのヘッダが要り、組み上げが脆くなるため）。
//
//  Apple の決まりで守っていること:
//    ・3.1.1  ゲーム内通貨は必ず In-App Purchase を通す
//    ・3.1.1  「一度買えば何度でも受け取れる」ので **非消耗型（Non-Consumable）**。
//             その場合「購入を復元する」を置くことが必須 → MTStoreRestore()
//    ・HIG    値段は App Store から受け取った現地表記（Product.displayPrice）を出す
//    ・HIG    支払いができない端末では商店を出さない → AppStore.canMakePayments
//    ・HIG    確認のシートは**作らない**。Product.purchase() が出す Apple のシートに任せる

import Foundation
import StoreKit
import UIKit

@available(iOS 15.0, *)
private final class MTStore {
    static let shared = MTStore()

    var products: [Product] = []
    var owned: Set<String> = []
    var ready = false
    var busy = false
    var message = ""
    private var updates: Task<Void, Never>?
    private var storefronts: Task<Void, Never>?
    /// 最後に読んだ品目の並び（読み直しに使う）。
    private var wanted: [String] = []

    /// 品目を読み、いま持っている権利を数え、以後の更新も見張る。
    func start(ids: [String]) {
        guard !ids.isEmpty else { return }
        wanted = ids
        busy = true
        // ★**店の国が変わったら値段を読み直す**。
        //   値段の字は Apple から受け取ったものをそのまま出しているので、
        //   読んだあとに国が変わると、並んでいる値段（$0.99）と
        //   実際に請求される額（¥150）が食い違って見える。
        //   サンドボックスでは、あとからテスト用アカウントに入ると国が変わる。
        if storefronts == nil {
            storefronts = Task.detached { [weak self] in
                for await _ in Storefront.updates {
                    await self?.reload()
                }
            }
        }
        // 途中で買い直された／家族から共有されたぶんも拾えるように見張る
        if updates == nil {
            updates = Task.detached { [weak self] in
                for await update in Transaction.updates {
                    if case .verified(let t) = update {
                        await t.finish()
                        await self?.refreshOwned()
                    }
                }
            }
        }
        Task {
            do {
                let got = try await Product.products(for: ids)
                // 並びは渡した順（値段の安い順）に揃える
                self.products = ids.compactMap { id in got.first { $0.id == id } }
                await self.refreshOwned()
                self.ready = true
            } catch {
                self.message = "品目を読めませんでした。"
            }
            self.busy = false
        }
    }

    /// 品目を読み直す（店の国が変わったとき・窓を開けるとき）。
    func reload() async {
        guard !wanted.isEmpty else { return }
        busy = true
        do {
            let got = try await Product.products(for: wanted)
            self.products = wanted.compactMap { id in got.first { $0.id == id } }
            await self.refreshOwned()
            self.ready = true
        } catch {
            self.message = "品目を読めませんでした。"
        }
        busy = false
    }

    /// いま持っている権利（非消耗型なので、買ってあれば必ずここに出る）。
    func refreshOwned() async {
        var now: Set<String> = []
        for await entitlement in Transaction.currentEntitlements {
            if case .verified(let t) = entitlement, t.revocationDate == nil {
                now.insert(t.productID)
            }
        }
        owned = now
    }

    func buy(_ id: String) {
        guard let product = products.first(where: { $0.id == id }), !busy else { return }
        busy = true
        message = ""
        Task {
            do {
                // ★ここで Apple の確認シートが出る。自前で作ってはいけない。
                switch try await product.purchase() {
                case .success(let verification):
                    if case .verified(let t) = verification {
                        await t.finish()
                        await self.refreshOwned()
                    } else {
                        self.message = "購入を確かめられませんでした。"
                    }
                case .userCancelled:
                    break
                case .pending:
                    self.message = "承認を待っています。"
                @unknown default:
                    break
                }
            } catch {
                self.message = "購入できませんでした。"
            }
            self.busy = false
        }
    }

    /// 購入を復元する（非消耗型に必須。審査指針 3.1.1）。
    func restore() {
        guard !busy else { return }
        busy = true
        message = ""
        Task {
            do { try await AppStore.sync() } catch { /* 取り消しは知らせない */ }
            await self.refreshOwned()
            self.busy = false
        }
    }

    func json() -> String {
        var items: [String] = []
        for p in products {
            items.append("{\"id\":\(quote(p.id)),"
                       + "\"price\":\(quote(p.displayPrice)),"
                       + "\"owned\":\(owned.contains(p.id) ? "true" : "false")}")
        }
        return "{\"ready\":\(ready ? "true" : "false"),"
             + "\"canPay\":\(AppStore.canMakePayments ? "true" : "false"),"
             + "\"busy\":\(busy ? "true" : "false"),"
             + "\"msg\":\(quote(message)),"
             + "\"items\":[\(items.joined(separator: ","))]}"
    }

    private func quote(_ s: String) -> String {
        var out = "\""
        for c in s.unicodeScalars {
            switch c {
            case "\"": out += "\\\""
            case "\\": out += "\\\\"
            case "\n": out += "\\n"
            default:
                if c.value < 0x20 { out += String(format: "\\u%04x", c.value) }
                else { out.unicodeScalars.append(c) }
            }
        }
        return out + "\""
    }
}

// ------------------------------------------------------------------ C の口

private var mtBuffer: UnsafeMutablePointer<CChar>?

private func mtReturn(_ s: String) -> UnsafeMutablePointer<CChar>? {
    if let old = mtBuffer { free(old) }
    mtBuffer = strdup(s)
    return mtBuffer
}

@_cdecl("MTStoreStart")
public func MTStoreStart(_ csv: UnsafePointer<CChar>?) {
    guard #available(iOS 15.0, *), let csv = csv else { return }
    let ids = String(cString: csv).split(separator: ",").map(String.init)
    MTStore.shared.start(ids: ids)
}

@_cdecl("MTStorePoll")
public func MTStorePoll() -> UnsafeMutablePointer<CChar>? {
    guard #available(iOS 15.0, *) else {
        return mtReturn("{\"ready\":false,\"canPay\":false,\"busy\":false,\"msg\":\"\",\"items\":[]}")
    }
    return mtReturn(MTStore.shared.json())
}

@_cdecl("MTStoreBuy")
public func MTStoreBuy(_ id: UnsafePointer<CChar>?) {
    guard #available(iOS 15.0, *), let id = id else { return }
    MTStore.shared.buy(String(cString: id))
}

/// 品目を読み直す（窓を開けるとき・アプリが前に戻ったときに呼ぶ）。
/// 店の国が変わっていれば値段の字も入れ替わる。
@_cdecl("MTStoreReload")
public func MTStoreReload() {
    guard #available(iOS 15.0, *) else { return }
    Task { await MTStore.shared.reload() }
}

/// ★**評価をお願いする窓**（作者の指定。原典に無い）。
///
/// 出すのは **iOS 側の決まった窓**で、文面も星の数もボタンも Apple のもの。
/// アプリからは中身を変えられない（真似た窓を自前で作ることも禁じられている）。
/// 出るかどうかも OS が決める（同じ人には年に3回まで。出ないこともある）。
/// **TestFlight で入れたビルドでは何も出ない**（Apple がそう決めている）。
///
/// HIG: 押したら出るボタンにはしない。区切りのよい所で静かに呼ぶだけにする。
@_cdecl("MTRequestReview")
public func MTRequestReview() {
    DispatchQueue.main.async {
        guard let scene = UIApplication.shared.connectedScenes
                .first(where: { $0.activationState == .foregroundActive })
                as? UIWindowScene else { return }
        SKStoreReviewController.requestReview(in: scene)
    }
}

@_cdecl("MTStoreRestore")
public func MTStoreRestore() {
    guard #available(iOS 15.0, *) else { return }
    MTStore.shared.restore()
}
