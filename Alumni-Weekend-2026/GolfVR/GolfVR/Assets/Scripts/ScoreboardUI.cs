using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GolfVR
{
    public class ScoreboardUI : MonoBehaviour
    {
        [Header("Header & Event Title")]
        public Text titleText;
        public Text subtitleText;

        [Header("Hole Rows (1 to 9)")]
        public Text[] holeParTexts = new Text[9];
        public Text[] holeStrokeTexts = new Text[9];
        public Text[] holeStatusTexts = new Text[9];

        [Header("Total & Current Status")]
        public Text totalScoreText;
        public Text currentHoleIndicatorText;

        [Header("Celebration / Notification Banner")]
        public Text bannerText;
        public GameObject bannerPanel;

        private Coroutine _bannerCoroutine;
        private bool _builtDynamicUI = false;

        private void Awake()
        {
            EnsureUIComponents();

            if (titleText != null)
            {
                titleText.text = "University of Pittsburgh at Bradford";
            }
            if (subtitleText != null)
            {
                subtitleText.text = "Alumni Weekend 2026 • 9-Hole VR Mini Golf";
            }
        }

        private void EnsureUIComponents()
        {
            if (titleText != null && totalScoreText != null
                && holeParTexts != null && holeParTexts.Length > 0 && holeParTexts[0] != null)
            {
                return;
            }
            if (_builtDynamicUI) return;
            _builtDynamicUI = true;

            // These arrays are sized by the field initializer (new Text[9]) in
            // code, but a scene can persist a serialized override (e.g. an
            // empty array from when the component was first added) that wins
            // over the initializer -- reallocate to the size the row-building
            // loop below actually needs so it can't index out of range.
            if (holeParTexts == null || holeParTexts.Length < 9) holeParTexts = new Text[9];
            if (holeStrokeTexts == null || holeStrokeTexts.Length < 9) holeStrokeTexts = new Text[9];
            if (holeStatusTexts == null || holeStatusTexts.Length < 9) holeStatusTexts = new Text[9];

            // Setup or find World Space Canvas
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
            }

            RectTransform canvasRt = GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(1000f, 750f);
            canvasRt.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);

            CanvasScaler scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 3f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            Font standardFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Background panel
            GameObject bgObj = new GameObject("ScoreboardBG", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.0f, 0.13f, 0.38f, 0.94f); // Pitt Blue

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(bgObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0f, 320f);
            titleRt.sizeDelta = new Vector2(920f, 60f);
            titleText = titleObj.GetComponent<Text>();
            titleText.font = standardFont;
            titleText.fontSize = 38;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1.0f, 0.72f, 0.11f); // Pitt Gold

            // Subtitle
            GameObject subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
            subObj.transform.SetParent(bgObj.transform, false);
            RectTransform subRt = subObj.GetComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0f, 270f);
            subRt.sizeDelta = new Vector2(920f, 40f);
            subtitleText = subObj.GetComponent<Text>();
            subtitleText.font = standardFont;
            subtitleText.fontSize = 24;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = Color.white;

            // Indicator
            GameObject indObj = new GameObject("CurrentHole", typeof(RectTransform), typeof(Text));
            indObj.transform.SetParent(bgObj.transform, false);
            RectTransform indRt = indObj.GetComponent<RectTransform>();
            indRt.anchoredPosition = new Vector2(0f, 220f);
            indRt.sizeDelta = new Vector2(920f, 40f);
            currentHoleIndicatorText = indObj.GetComponent<Text>();
            currentHoleIndicatorText.font = standardFont;
            currentHoleIndicatorText.fontSize = 26;
            currentHoleIndicatorText.fontStyle = FontStyle.Bold;
            currentHoleIndicatorText.alignment = TextAnchor.MiddleCenter;
            currentHoleIndicatorText.color = new Color(0.85f, 0.92f, 1.0f);

            // Table Header
            GameObject hdrObj = new GameObject("TableHeader", typeof(RectTransform), typeof(Text));
            hdrObj.transform.SetParent(bgObj.transform, false);
            RectTransform hdrRt = hdrObj.GetComponent<RectTransform>();
            hdrRt.anchoredPosition = new Vector2(0f, 170f);
            hdrRt.sizeDelta = new Vector2(860f, 30f);
            Text hdrText = hdrObj.GetComponent<Text>();
            hdrText.font = standardFont;
            hdrText.fontSize = 20;
            hdrText.fontStyle = FontStyle.Bold;
            hdrText.alignment = TextAnchor.MiddleLeft;
            hdrText.color = new Color(1.0f, 0.72f, 0.11f); // Pitt Gold
            hdrText.text = "  HOLE           PAR        STROKES       STATUS";

            // Rows for Holes 1 through 9
            float startY = 130f;
            float rowSpacing = 36f;

            for (int i = 0; i < 9; i++)
            {
                float rowY = startY - (i * rowSpacing);

                // Row Container
                GameObject rowObj = new GameObject($"Row_Hole{i + 1}", typeof(RectTransform));
                rowObj.transform.SetParent(bgObj.transform, false);
                RectTransform rowRt = rowObj.GetComponent<RectTransform>();
                rowRt.anchoredPosition = new Vector2(0f, rowY);
                rowRt.sizeDelta = new Vector2(860f, 32f);

                // Par Text
                GameObject pObj = new GameObject("Par", typeof(RectTransform), typeof(Text));
                pObj.transform.SetParent(rowObj.transform, false);
                RectTransform pRt = pObj.GetComponent<RectTransform>();
                pRt.anchoredPosition = new Vector2(-220f, 0f);
                pRt.sizeDelta = new Vector2(380f, 30f);
                Text pText = pObj.GetComponent<Text>();
                pText.font = standardFont;
                pText.fontSize = 20;
                pText.alignment = TextAnchor.MiddleLeft;
                pText.color = Color.white;
                holeParTexts[i] = pText;

                // Strokes Text
                GameObject sObj = new GameObject("Strokes", typeof(RectTransform), typeof(Text));
                sObj.transform.SetParent(rowObj.transform, false);
                RectTransform sRt = sObj.GetComponent<RectTransform>();
                sRt.anchoredPosition = new Vector2(60f, 0f);
                sRt.sizeDelta = new Vector2(120f, 30f);
                Text sText = sObj.GetComponent<Text>();
                sText.font = standardFont;
                sText.fontSize = 20;
                sText.fontStyle = FontStyle.Bold;
                sText.alignment = TextAnchor.MiddleCenter;
                sText.color = Color.white;
                holeStrokeTexts[i] = sText;

                // Status Text
                GameObject stObj = new GameObject("Status", typeof(RectTransform), typeof(Text));
                stObj.transform.SetParent(rowObj.transform, false);
                RectTransform stRt = stObj.GetComponent<RectTransform>();
                stRt.anchoredPosition = new Vector2(260f, 0f);
                stRt.sizeDelta = new Vector2(180f, 30f);
                Text stText = stObj.GetComponent<Text>();
                stText.font = standardFont;
                stText.fontSize = 20;
                stText.fontStyle = FontStyle.Bold;
                stText.alignment = TextAnchor.MiddleCenter;
                stText.color = Color.white;
                holeStatusTexts[i] = stText;
            }

            // Total Score Row
            GameObject totObj = new GameObject("TotalScore", typeof(RectTransform), typeof(Text));
            totObj.transform.SetParent(bgObj.transform, false);
            RectTransform totRt = totObj.GetComponent<RectTransform>();
            totRt.anchoredPosition = new Vector2(0f, -225f);
            totRt.sizeDelta = new Vector2(900f, 50f);
            totalScoreText = totObj.GetComponent<Text>();
            totalScoreText.font = standardFont;
            totalScoreText.fontSize = 26;
            totalScoreText.fontStyle = FontStyle.Bold;
            totalScoreText.alignment = TextAnchor.MiddleCenter;
            totalScoreText.color = new Color(1.0f, 0.72f, 0.11f); // Pitt Gold

            // Floating Celebration / Notification Banner Panel
            bannerPanel = new GameObject("BannerPanel", typeof(RectTransform), typeof(Image));
            bannerPanel.transform.SetParent(bgObj.transform, false);
            RectTransform bnrRt = bannerPanel.GetComponent<RectTransform>();
            bnrRt.anchoredPosition = new Vector2(0f, -300f);
            bnrRt.sizeDelta = new Vector2(920f, 75f);
            Image bnrImg = bannerPanel.GetComponent<Image>();
            bnrImg.color = new Color(0.04f, 0.08f, 0.16f, 0.95f);

            GameObject bnrTextObj = new GameObject("BannerText", typeof(RectTransform), typeof(Text));
            bnrTextObj.transform.SetParent(bannerPanel.transform, false);
            RectTransform bnrTextRt = bnrTextObj.GetComponent<RectTransform>();
            bnrTextRt.anchorMin = Vector2.zero;
            bnrTextRt.anchorMax = Vector2.one;
            bnrTextRt.sizeDelta = Vector2.zero;
            bannerText = bnrTextObj.GetComponent<Text>();
            bannerText.font = standardFont;
            bannerText.fontSize = 24;
            bannerText.fontStyle = FontStyle.Bold;
            bannerText.alignment = TextAnchor.MiddleCenter;
            bannerText.color = Color.white;

            bannerPanel.SetActive(false);
        }

        public void UpdateScoreboard(GolfHole[] holes, int[] strokes, int currentHoleIndex)
        {
            EnsureUIComponents();

            int totalPar = 0;
            int totalStrokes = 0;
            int numHoles = holes != null ? holes.Length : 9;

            for (int i = 0; i < numHoles; i++)
            {
                int par = (holes != null && i < holes.Length && holes[i] != null) ? holes[i].par : 2;
                int strokeCount = (strokes != null && i < strokes.Length) ? strokes[i] : 0;

                totalPar += par;
                totalStrokes += strokeCount;

                string hName = (holes != null && i < holes.Length && holes[i] != null && !string.IsNullOrEmpty(holes[i].holeName))
                    ? holes[i].holeName
                    : $"Hole {i + 1}";

                if (holeParTexts != null && i < holeParTexts.Length && holeParTexts[i] != null)
                {
                    holeParTexts[i].text = $"Hole {i + 1}: {hName} (Par {par})";
                }

                if (holeStrokeTexts != null && i < holeStrokeTexts.Length && holeStrokeTexts[i] != null)
                {
                    holeStrokeTexts[i].text = strokeCount > 0 ? strokeCount.ToString() : "-";
                }

                if (holeStatusTexts != null && i < holeStatusTexts.Length && holeStatusTexts[i] != null)
                {
                    if (i < currentHoleIndex)
                    {
                        int diff = strokeCount - par;
                        holeStatusTexts[i].text = diff == 0 ? "E" : (diff > 0 ? $"+{diff}" : $"{diff}");
                        holeStatusTexts[i].color = diff <= 0 ? new Color(0.1f, 0.85f, 0.25f) : new Color(0.95f, 0.35f, 0.25f);
                    }
                    else if (i == currentHoleIndex)
                    {
                        holeStatusTexts[i].text = "Playing";
                        holeStatusTexts[i].color = new Color(1.0f, 0.72f, 0.11f); // Pitt Gold
                    }
                    else
                    {
                        holeStatusTexts[i].text = "Upcoming";
                        holeStatusTexts[i].color = new Color(0.6f, 0.65f, 0.75f);
                    }
                }
            }

            if (totalScoreText != null)
            {
                int totalDiff = totalStrokes - totalPar;
                string diffStr = totalDiff == 0 ? "E" : (totalDiff > 0 ? $"+{totalDiff}" : $"{totalDiff}");
                totalScoreText.text = $"Total Strokes: {totalStrokes}  (Par {totalPar} | {diffStr})";
            }

            if (currentHoleIndicatorText != null)
            {
                currentHoleIndicatorText.text = $"CURRENT: HOLE {currentHoleIndex + 1} of {numHoles}";
            }
        }

        public void ShowBanner(string message, float duration = 3.0f)
        {
            EnsureUIComponents();

            if (bannerText != null)
            {
                bannerText.text = message;
            }

            if (bannerPanel != null)
            {
                bannerPanel.SetActive(true);
            }

            if (_bannerCoroutine != null)
            {
                StopCoroutine(_bannerCoroutine);
            }

            _bannerCoroutine = StartCoroutine(HideBannerAfterDelay(duration));
        }

        private IEnumerator HideBannerAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (bannerPanel != null)
            {
                bannerPanel.SetActive(false);
            }
        }
    }
}
