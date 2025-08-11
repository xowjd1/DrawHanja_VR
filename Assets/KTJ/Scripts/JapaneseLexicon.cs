using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class JapaneseLexicon
{
    // 너의 학습 셋(기초 한자) 위주 동의 표기 → 히라가나
    // 필요 시 더 추가
    static readonly Dictionary<string, string> MapToHiragana = new(StringComparer.OrdinalIgnoreCase)
    {
        // 火
        { "火", "ひ" }, { "hi", "ひ" }, { "ひ", "ひ" }, { "ヒ", "ひ" }, { "히", "ひ" },

        // 水
        { "水", "みず" }, { "mizu", "みず" }, { "みず", "みず" }, { "ミズ", "みず" }, { "미즈", "みず" },

        // 木
        { "木", "き" }, { "ki", "き" }, { "き", "き" }, { "キ", "き" }, { "키", "き" },

        // 力 (훈: ちから / 음: りょく)
        { "力", "ちから" }, { "chikara", "ちから" }, { "ちから", "ちから" }, { "チカラ", "ちから" }, { "치카라", "ちから" },
        { "ryoku", "りょく" }, { "りょく", "りょく" }, { "リョク", "りょく" }, { "료쿠", "りょく" },

        // 風
        { "風", "かぜ" }, { "kaze", "かぜ" }, { "かぜ", "かぜ" }, { "カゼ", "かぜ" }, { "카제", "かぜ" },

        // 斬
        { "斬",   "ざん" },   // 단독 한자 입력은 기본 음독으로 처리 (원하면 "きる"로 바꿔도 OK)
        { "ざん", "ざん" }, { "ザン", "ざん" }, { "zan", "ざん" }, { "잔", "ざん" },

        { "きる", "きる" }, { "キル", "きる" }, { "kiru", "きる" }, { "키루", "きる" },
        { "き-る","きる" }, // 하이픈/장음 등은 정규화에서 제거되지만 안전하게 추가
        { "斬る", "きる" },

        // 猿
        { "猿", "さる" }, { "saru", "さる" }, { "さる", "さる" }, { "サル", "さる" }, { "사루", "さる" },

        // 鳥
        { "鳥", "とり" }, { "tori", "とり" }, { "とり", "とり" }, { "トリ", "とり" }, { "토리", "とり" },

        // 船（훈: ふね / 음: せん）
        { "船", "ふね" }, { "fune", "ふね" }, { "ふね", "ふね" }, { "フネ", "ふね" }, { "후네", "ふね" },
        { "sen", "せん" }, { "せん", "せん" }, { "セン", "せん" }, { "센", "せん" },

        // 犬
        { "犬", "いぬ" }, { "inu", "いぬ" }, { "いぬ", "いぬ" }, { "イヌ", "いぬ" }, { "이누", "いぬ" },

        // 光
        { "光", "ひかり" }, { "hikari", "ひかり" }, { "ひかり", "ひかり" }, { "ヒカリ", "ひかり" }, { "히카리", "ひかり" },

        // 炎
        { "炎", "ほのお" }, { "honoo", "ほのお" }, { "ほのお", "ほのお" }, { "ホノオ", "ほのお" }, { "호노오", "ほのお" },

        // 石
        { "石", "いし" }, { "ishi", "いし" }, { "いし", "いし" }, { "イシ", "いし" }, { "이시", "いし" },

        // 刀（훈: かたな / 음: とう）
        { "刀", "かたな" }, { "katana", "かたな" }, { "かたな", "かたな" }, { "カタナ", "かたな" }, { "카타나", "かたな" },
        { "tou", "とう" }, { "とう", "とう" }, { "トウ", "とう" }, { "토우", "とう" },
    };

    public static string NormalizeForMatching(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string s = input.Trim();
        s = RemoveSpacesAndPunct(s);

        // 1) 맵에 바로 있으면 끝
        if (MapToHiragana.TryGetValue(s, out var mapped))
            return mapped;

        // 2) 카타카나 → 히라가나
        string kana = KatakanaToHiragana(s);

        if (MapToHiragana.TryGetValue(kana, out mapped))
            return mapped;

        // 3) 한자 단일 매핑 (셋에 없는 한자는 그대로)
        if (MapToHiragana.TryGetValue(OnlyKanji(s), out mapped))
            return mapped;

        // 4) 로마자/한글을 간단 변환 (아주 제한적으로)
        string rom = HangulToRomajiLite(s).ToLowerInvariant();
        if (MapToHiragana.TryGetValue(rom, out mapped))
            return mapped;

        // 5) 마지막으로 히라가나 범위만 남기기
        string onlyKana = KeepHiragana(kana);
        return string.IsNullOrEmpty(onlyKana) ? s : onlyKana;
    }

    static string RemoveSpacesAndPunct(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (char.IsWhiteSpace(ch)) continue;
            if ("、。.,!！?？ー-~〜".IndexOf(ch) >= 0) continue;
            sb.Append(ch);
        }
        return sb.ToString();
    }

    static string KatakanaToHiragana(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            // U+30A1..U+30F6 => U+3041..U+3096 (0x60 차이)
            if (ch >= 0x30A1 && ch <= 0x30F6) sb.Append((char)(ch - 0x60));
            else sb.Append(ch);
        }
        // 장음부호 ー 간단 치환(대략): か → かあ … 등은 케이스마다 다르지만 여기선 제거
        return sb.ToString().Replace("ー", "");
    }

    static string OnlyKanji(string s)
    {
        // 아주 단순: 한 글자 한자만 취급
        if (s.Length == 1)
        {
            char ch = s[0];
            if ((ch >= 0x4E00 && ch <= 0x9FFF) || (ch >= 0x3400 && ch <= 0x4DBF))
                return s;
        }
        return s;
    }

    // 정말 최소한의 한글→로마자 (데모용; 필요한 것만 커버)
    static string HangulToRomajiLite(string s)
    {
        // 자주 쓰는 것만 간단 매핑
        var direct = new Dictionary<string,string>(StringComparer.Ordinal)
        {
            { "히", "hi" }, { "미즈", "mizu" }, { "키", "ki" }, { "료쿠", "ryoku" }, { "치카라", "chikara" },
            { "카제", "kaze" }, { "잔", "zan" }, { "사루", "saru" }, { "토리", "tori" },
            { "후네", "fune" }, { "센", "sen" }, { "이누", "inu" }, { "히카리", "hikari" },
            { "호노오", "honoo" }, { "이시", "ishi" }, { "카타나", "katana" }, { "토우", "tou" },
        };
        if (direct.TryGetValue(s, out var r)) return r;
        return s; // 그 외는 그대로
    }

    static string KeepHiragana(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (ch >= 0x3040 && ch <= 0x309F) sb.Append(ch);
        }
        return sb.ToString();
    }
}
