# HOLLOW ATLAS — HUD Premium Tasarim Implementasyon Brief'i
**Hedef:** Hades 2 ilhamli, daha rafine bir HUD. Dairesel HP orb + premium XP bar + level + altin sayaci.
**Brief sahibi:** Claude (claude.ai) — tasarim katmani
**Uygulayacak:** Claude Code — Unity tarafi

---

## 1. GENEL VIZYON

```
+------------------+
|   HP ORB (130px) |   <- Dairesel, mor/siyah orb icinde HP yuzdesi
|   Vita yazisi    |
+------------------+
        |
        +-- yaninda saga dogru:
            +------------------+
            | [III] Atlas Lvl |   <- Roma rakami + label
            +------------------+
            | [============]  |   <- XP bar (mor->cyan gradient)
            | EXPERIENCE 120/300
            +------------------+
            | (o) 247         |   <- Altin pip
            +------------------+
```

Sol ust kose. Mevcut HUD'in HP/XP barlarinin yerini alacak.

---

## 2. RENK PALETI (HEX — HDR ICIN BLOOM ENABLE)

### HP Orb (kirmizi ramp)
- HP fill bright:    `#FF6B78` (HDR x1.5 — Bloom yer)
- HP fill mid:       `#DC3545`
- HP fill dark:      `#6A1020`
- Orb iç gölge:      `#08040E` (rgba 0.7)
- Orb dis cember:    `#B482DC` (rgba 0.4)
- Orb track (bos):   `#28143A` (rgba 0.95)

### XP Bar (mor → cyan)
- XP start (sol):    `#5A1F9C`
- XP mid:            `#9A3DD8`
- XP late:           `#C862EE`
- XP cyan transition: `#6DD9F0`
- XP bright cyan:    `#A8E8FF` (HDR x1.3)
- Bar arka plan:     `#08041A` (rgba 0.9)
- Bar cerceve:       `#B482DC` (rgba 0.5)

### Level Rozeti
- Iç dolgu mor:      `#8C50C8` → `#1E0F37` (radial)
- Cerceve:           `#DCB4FF` (rgba 0.7)
- Yazi rengi:        `#F5E0FF`
- Pulse halka:       `#DCB4FF` (rgba 0.3)

### Gold
- Coin parlak:       `#FFE79B`
- Coin orta:         `#F4C64A`
- Coin koyu:         `#8A5A10`
- Yazi:              `#FFE5A0`

### Genel HUD Background (hafif overlay icin)
- Mor sis:           `#251A40` (rgba 0.22)
- Cyan sis:          `#0D2540` (rgba 0.14)

---

## 3. UNITY HIERARCHY

```
Canvas (Screen Space - Overlay)
└── HUD_TopLeft  (RectTransform, anchored top-left, pos: 30, -30)
    ├── HP_Orb_Group  (RectTransform: 130x130)
    │   ├── Orb_Background_Glow      (Image — radial mor halo)
    │   ├── Orb_Rotating_Ring        (Image — dashed ring, dondurulecek)
    │   ├── Orb_Track                (Image — bos kalip, koyu mor)
    │   ├── Orb_Fill                 (Image — Filled, Radial 360, HP %)
    │   ├── Orb_Inner_Disk           (Image — ic disk, koyu)
    │   ├── Orb_Cardinal_Marks       (Image — 4 yon cizgileri)
    │   └── Orb_Text_Group
    │       ├── HP_Number            (TMP_Text, 28px, Cinzel)
    │       └── HP_Label_Vita        (TMP_Text, 8px, Cinzel)
    │
    └── Right_Stack  (Vertical Layout Group, gap: 10)
        ├── Level_Row  (Horizontal Layout)
        │   ├── Level_Symbol_Bg      (Image — radial circle, mor)
        │   ├── Level_Symbol_Pulse   (Image — pulse halka, animator)
        │   ├── Level_Roman_Text     (TMP_Text — "III")
        │   └── Level_Label_Text     (TMP_Text — "ATLAS LEVEL")
        │
        ├── XP_Bar_Group
        │   ├── XP_Bar_Frame         (Image — dis cerceve)
        │   ├── XP_Bar_Background    (Image — bos siyah)
        │   ├── XP_Bar_Fill          (Image — Filled, Horizontal, sol→sag)
        │   ├── XP_Flow_Effect       (Image — beyaz seffaf serit, scrolling)
        │   └── XP_Meta_Text_Row
        │       ├── XP_Label_Text    ("EXPERIENCE")
        │       └── XP_Value_Text    ("120 / 300")
        │
        └── Gold_Pip
            ├── Coin_Image           (Image — radial gold)
            └── Gold_Number          (TMP_Text — "247")
```

---

## 4. UI IMAGE AYARLARI (UNITY INSPECTOR)

### Orb_Fill (HP yuzdesi gosteren)
- Image Type: **Filled**
- Fill Method: **Radial 360**
- Fill Origin: **Top**
- Clockwise: **true**
- Fill Amount: 0–1 (script'ten guncellenir)
- Material: HDR Color destekli (Bloom icin)

### XP_Bar_Fill
- Image Type: **Filled**
- Fill Method: **Horizontal**
- Fill Origin: **Left**
- Fill Amount: 0–1

### Orb_Rotating_Ring
- Static SVG/PNG (dashed circle)
- Script ile transform.Rotate(0,0, -12 * Time.deltaTime) — 30sn'de 1 tur

---

## 5. FONT — CINZEL TMP

1. **Font indir:** Google Fonts → Cinzel (Regular + Medium 500)
2. Unity'e import: `Assets/Fonts/Cinzel-Regular.ttf`, `Cinzel-Medium.ttf`
3. **Window > TextMeshPro > Font Asset Creator**
4. Source Font: Cinzel-Regular
5. Atlas Resolution: 1024x1024
6. Character Set: ASCII + Roman numerals (I, II, III, IV, V, VI, VII, VIII, IX, X)
7. Olustur → `Assets/Fonts/TMP/Cinzel_SDF.asset`
8. Tum HUD TMP_Text'lerine bu asset'i ata

**Tipografi olcekleri:**
| Eleman | Boyut | Stil |
|---|---|---|
| HP_Number | 28pt | Medium |
| HP_Label_Vita | 8pt | Medium, letter-spacing 0.4em (TMP'de Character Spacing: 25) |
| Level_Roman | 14pt | Medium |
| Level_Label | 12pt | Medium, letter-spacing 0.25em |
| XP_Label | 9pt | Medium, letter-spacing 0.25em |
| XP_Value | 10pt | Medium |
| Gold_Number | 14pt | Medium |

**Text shadow:** TMP > Underlay tab → Color: koyu mor `#1A0A2E`, X: 0, Y: -1, Dilate: 0.3, Softness: 0.5

**Glow (ozellikle HP_Number icin):** TMP > Glow tab → Color: mor `#C864FF`, Power: 0.6, Inner: 0.3, Outer: 0.5

---

## 6. SCRIPTLER — DEGISIKLIK BRIEF'I

### A) Mevcut HUD scripti (PlayerHUD.cs veya HudController.cs adli scriptin)

Su anki HP/XP atama mantigini koru, ama **gorsel guncellemeyi smooth lerp ile yap**:

```csharp
// EKLE — ust kismina
[Header("Premium HUD Refs")]
[SerializeField] private Image hpOrbFill;
[SerializeField] private Image xpBarFill;
[SerializeField] private TMP_Text hpNumberText;
[SerializeField] private TMP_Text xpValueText;
[SerializeField] private TMP_Text levelRomanText;
[SerializeField] private float hudLerpSpeed = 8f;

private float currentHpFill = 1f;
private float currentXpFill = 0f;

// Update icindeki HP/XP atama satirlarini DEGISTIR:
private void Update()
{
    // Smooth lerp — ani degisim yerine
    float targetHp = (float)currentHP / maxHP;
    currentHpFill = Mathf.Lerp(currentHpFill, targetHp, Time.deltaTime * hudLerpSpeed);
    hpOrbFill.fillAmount = currentHpFill;

    float targetXp = (float)currentXP / xpToNext;
    currentXpFill = Mathf.Lerp(currentXpFill, targetXp, Time.deltaTime * hudLerpSpeed);
    xpBarFill.fillAmount = currentXpFill;

    // Numbers
    hpNumberText.text = currentHP.ToString();
    xpValueText.text = $"{currentXP} / {xpToNext}";
    levelRomanText.text = ToRoman(currentLevel);

    // Critical HP detection
    HandleCriticalHP(targetHp);
}

private void HandleCriticalHP(float hpRatio)
{
    if (hpRatio <= 0.25f && !isCritical)
    {
        isCritical = true;
        StartCoroutine(CriticalPulse());
    }
    else if (hpRatio > 0.25f && isCritical)
    {
        isCritical = false;
    }
}

private IEnumerator CriticalPulse()
{
    while (isCritical)
    {
        // Orb'u hizli nefes aldir — kalp atisi
        hpOrbFill.transform.parent.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.time * 8f));
        yield return null;
    }
    hpOrbFill.transform.parent.localScale = Vector3.one;
}

// Roma rakami helper
private string ToRoman(int num)
{
    string[] roman = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
                       "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX" };
    return num >= 0 && num < roman.Length ? roman[num] : num.ToString();
}
```

### B) YENI script: `OrbRingRotator.cs`

```csharp
using UnityEngine;

// Orb_Rotating_Ring objesine ata
public class OrbRingRotator : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = -12f;

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
```

### C) YENI script: `OrbBreathing.cs`

```csharp
using UnityEngine;

// HP_Orb_Group objesine ata — pasif (hep) nefes alma
public class OrbBreathing : MonoBehaviour
{
    [SerializeField] private float breathSpeed = 1.5f;
    [SerializeField] private float breathAmount = 0.015f;

    private Vector3 baseScale;

    private void Awake() => baseScale = transform.localScale;

    private void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * breathSpeed) * breathAmount;
        transform.localScale = baseScale * s;
    }
}
```
*Not:* Critical HP coroutine'i bu script'i gecici olarak override etmeli — hizlandirma icin coroutine sirasinda `OrbBreathing.enabled = false` yap, bittikten sonra `true`.

### D) YENI script: `XpBarFlowEffect.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;

// XP_Flow_Effect (RawImage onerilen) objesine ata
public class XpBarFlowEffect : MonoBehaviour
{
    [SerializeField] private RawImage flowImage;
    [SerializeField] private float scrollSpeed = 0.5f;

    private void Update()
    {
        if (flowImage == null) return;
        Rect uv = flowImage.uvRect;
        uv.x = (uv.x + scrollSpeed * Time.deltaTime) % 1f;
        flowImage.uvRect = uv;
    }
}
```
*Asset:* `XP_Flow_Effect` icin yatay beyaz-seffaf gradient PNG hazirla (256x16, alpha sol 0% → orta 60% → sag 0%). Wrap mode: Repeat.

### E) YENI script: `LevelPulseRing.cs` (opsiyonel, animator yerine)

```csharp
using UnityEngine;
using UnityEngine.UI;

public class LevelPulseRing : MonoBehaviour
{
    [SerializeField] private Image pulseImage;
    [SerializeField] private float speed = 2.5f;

    private void Update()
    {
        if (pulseImage == null) return;
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        Color c = pulseImage.color;
        c.a = Mathf.Lerp(0.3f, 0.7f, t);
        pulseImage.color = c;
        pulseImage.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.1f, t);
    }
}
```

---

## 7. POST-PROCESSING (URP)

URP Renderer'a **Bloom** ekle:
- Threshold: 1.0
- Intensity: 0.6
- Scatter: 0.7
- Tint: `#FFFFFF`

HDR renkler (HP fill, XP cyan ucu, Gold coin) Bloom ile parlayacak — Hades 2 hissi buradan geliyor.

---

## 8. SES TETIKLEYICILERI (opsiyonel ama tavsiye)

PlayerHUD.cs icinde:
- HP `<= 25%` ilk kez tetiklendiginde: dusuk ses tonu nabiz sesi
- Level up: mistik bell/glock sesi
- Gold artisi: hafif coin sesi

Mevcut SoundManager scriptin varsa oraya entegre.

---

## 9. UYGULAMA SIRASI (Claude Code icin onerilen sira)

1. **Cinzel font import + TMP asset olustur**
2. Hierarchy'i kur (yukaridaki agac)
3. Image'lara Filled type + ayarlari ata
4. Renkleri Inspector'dan gir (HDR icin Bloom Renderer aktif)
5. **PlayerHUD.cs** icinde lerp + roma + critical pulse mantigini entegre et
6. OrbRingRotator, OrbBreathing, XpBarFlowEffect, LevelPulseRing scriptlerini ekle ve assign et
7. URP Bloom ayarini yap
8. Run → "Yeni Run" / "Mid-Run" / "Kritik" / "Boss Sonrasi" durumlarini test et

---

## 10. TEST SENARYOLARI

| Senaryo | Beklenen Davranis |
|---|---|
| Run basi | HP %100, XP %0, Level I |
| Hasar al | Orb fill anlik degil, smooth azalir |
| HP %25 alti | Orb hizli nefes alir (kalp atisi) |
| XP toplama | XP bar smooth dolar, seffaf serit akar |
| Level up | Roma rakami artar, XP bar resetlenir |
| Altin topla | Gold sayaci anlik artar |
| Idle | Orb yavasca nefes alir, dis halka doner, level rozeti pulse atar |

---

## 11. PERFORMANS NOTLARI

- Tum animasyonlar `Update()` icinde — UI ogeleri icin sorun degil
- Coroutine'ler sadece critical HP'de aktif
- Eger 60+ FPS sorun olursa: `Update` yerine `LateUpdate` veya 30Hz interval

---

## 12. VARYASYON OPSIYONLARI (sonraki iterasyonlar)

- Orb'un icine seffaf "siv akiskan" hareketi (water shader)
- HP azaldikca orb cevresinde damla efekti (particle system)
- Level up'ta tum HUD'a flash mor patlama
- Boss savasinda HUD kosesinde ek "boss healthbar" — ayni stilde

Bu liste ileri iterasyonlar; ilk pas **temel implementasyon** odaklı.

---

*[Brief sonu — Claude Code bu dosyayi okudugu an PlayerHUD.cs ve ilgili dosyalari acip uygulamaya basliyabilir.]*
