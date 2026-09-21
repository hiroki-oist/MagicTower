using UnityEngine;

/// <summary>
/// マスの物を拾ったときの効果。原典 MTE.EXE のアイテム取得分岐（0x2574〜0x2f26）より。
///
/// 効果量は表ではなく**式**で決まる:
/// <code>
///   block = (現在階 - 1) / 10 + 1        ; 1..5
///   回復薬（赤） = block × 50
///   回復薬（青） = block × 200
///   クリスタル   = block                  ; 攻撃・防御とも
///   剣 = +10 / +20 / +40 / +50 / +100     ; 段階は物体IDの昇順
///   盾 = +10 / +20 / +40 / +50 / +100
/// </code>
///
/// 特殊アイテムは所持フラグが立つだけで、効果は使うときに出る
/// （<see cref="SpecialItemEffects"/>）。ただし
/// 十字架・ドラゴンスレイヤー・幸運の金貨は持っているだけで効く。
/// </summary>
public static class ItemPickup
{
    public struct Result
    {
        public bool Taken;
        /// <summary>Messages.txt のキー。無ければ null。</summary>
        public string MessageKey;
        /// <summary>メッセージに埋める数値。</summary>
        public int Value;
    }

    static Result None() { return new Result { Taken = false }; }
    static Result Ok(string key, int value = 0)
    {
        return new Result { Taken = true, MessageKey = key, Value = value };
    }

    /// <summary>装備の段階ごとの上昇値。原典の即値そのまま。</summary>
    public static readonly int[] EquipGain = { 0, 10, 20, 40, 50, 100 };

    public static Result Apply(PlayerState player, string prefabName, ItemPowerTable power)
    {
        if (player == null || string.IsNullOrEmpty(prefabName)) return None();
        var band = power != null ? power.ForFloor(player.Floor) : default(ItemPowerTable.Band);

        switch (prefabName)
        {
            case "Key_Yellow": player.YellowKeys++; return Ok("key_yellow");
            case "Key_Red":    player.RedKeys++;    return Ok("key_red");
            case "Key_Blue":   player.PurpleKeys++; return Ok("key_purple");   // 原典の「紫の鍵」

            case "Potion_Red":  player.Heal(band.PotionRed);  return Ok("potion", band.PotionRed);
            case "Potion_Blue": player.Heal(band.PotionBlue); return Ok("potion", band.PotionBlue);

            case "Gem_Attack":
                player.Attack += band.CrystalAttack;
                return Ok("crystal_atk", band.CrystalAttack);
            case "Gem_Defense":
                player.Defense += band.CrystalDefense;
                return Ok("crystal_def", band.CrystalDefense);
        }

        int rank;
        if (TryRank(prefabName, "Sword_", out rank))
        {
            player.Attack += EquipGain[rank];
            player.SwordRank = rank;
            return Ok("sword_" + rank);
        }
        if (TryRank(prefabName, "Shield_", out rank))
        {
            player.Defense += EquipGain[rank];
            player.ShieldRank = rank;
            return Ok("shield_" + rank);
        }

        var kind = SpecialItems.FromPrefabName(prefabName);
        if (kind != SpecialItems.Kind.None)
        {
            player.Give(kind);
            return Ok(MessageKeyOf(kind));
        }

        return None();
    }

    static bool TryRank(string name, string prefix, out int rank)
    {
        rank = 0;
        if (!name.StartsWith(prefix)) return false;
        var tail = name.Substring(prefix.Length);
        return int.TryParse(tail, out rank) && rank >= 1 && rank <= 5;
    }

    static string MessageKeyOf(SpecialItems.Kind k)
    {
        switch (k)
        {
            case SpecialItems.Kind.HeroOrb:        return "orb_hero";
            case SpecialItems.Kind.WisdomOrb:      return "orb_wisdom";
            case SpecialItems.Kind.FlightOrb:      return "orb_flight";
            case SpecialItems.Kind.Cross:          return "cross";
            case SpecialItems.Kind.Elixir:         return "elixir";
            case SpecialItems.Kind.Matock:         return "matock";
            case SpecialItems.Kind.DestructionOrb: return "destruction";
            case SpecialItems.Kind.TeleportRelic:  return "teleport";
            case SpecialItems.Kind.WingUp:         return "wing_up";
            case SpecialItems.Kind.WingDown:       return "wing_down";
            case SpecialItems.Kind.DragonSlayer:   return "dragon_slayer";
            case SpecialItems.Kind.SnowCrystal:    return "snow_crystal";
            case SpecialItems.Kind.MagicKey:       return "magic_key";
            case SpecialItems.Kind.SuperMatock:    return "super_matock";
            case SpecialItems.Kind.LuckyCoin:      return "lucky_coin";
            default: return null;
        }
    }
}
