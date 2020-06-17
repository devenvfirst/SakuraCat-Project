// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class RevealableUIText : Text, IRevealableText
    {
        protected readonly struct CharInfo
        {
            public static readonly CharInfo Invalid = new CharInfo(-1, -1, new UICharInfo { charWidth = 0 }, default);

            public readonly int CharIndex;
            public readonly int LineIndex;
            public readonly UICharInfo Char;
            public readonly UILineInfo Line;

            public bool Visible => Char.charWidth > 0;
            public float Origin => Char.cursorPos.x;
            public float XAdvance => Char.cursorPos.x + Char.charWidth;
            public float Ascender => Line.topY;

            public CharInfo (int charIndex, int lineIndex, UICharInfo @char, UILineInfo line)
            {
                CharIndex = charIndex;
                LineIndex = lineIndex;
                Char = @char;
                Line = line;
            }
        }

        public virtual string Text { get => text; set { text = value; rebuildPending = rebuildPending || rectTransform.hasChanged; } }
        public virtual Color TextColor { get => color; set => color = value; }
        public virtual GameObject GameObject => gameObject;
        public virtual float RevealProgress { get => GetRevealProgress(); set => SetRevealProgress(value); }
        public virtual bool Revealing => revealState.InProgress;

        protected virtual int LastRevealedVisibleCharIndex { get; private set; }
        protected virtual int LastVisibleCharIndex { get; private set; }
        protected virtual Transform CanvasTransform => canvasTransformCache ? canvasTransformCache : (canvasTransformCache = canvas.GetComponent<Transform>());
        protected virtual float SlideProgress => slideClipRect && lastRevealDuration > 0 ? Mathf.Clamp01((Time.time - lastRevealTime) / lastRevealDuration) : 1f;

        protected float RevealFadeWidth => revealFadeWidth;
        protected bool SlideClipRect => slideClipRect;
        protected float ItalicSlantAngle => italicSlantAngle;

        [Tooltip("Width (in pixels) of the gradient fade near the reveal border.")]
        [SerializeField] private float revealFadeWidth = 100f;
        [Tooltip("Whether to smoothly reveal the text. Disable for the `typewriter` effect.")]
        [SerializeField] private bool slideClipRect = true;
        [Tooltip("How much to slant the reveal rect to compensate for italic characters; 10 is usually enough for most fonts.\n\nNotice, that enabling the slanting (value greater than zero) would introduce minor reveal effect artifacts. TMPro printers are not affected by this issue, so consider using them instead.")]
        [SerializeField] private float italicSlantAngle = 0f;

        private const string textShaderName = "Naninovel/RevealableText";
        private static readonly int lineClipRectPropertyId = Shader.PropertyToID("_LineClipRect");
        private static readonly int charClipRectPropertyId = Shader.PropertyToID("_CharClipRect");
        private static readonly int charFadeWidthPropertyId = Shader.PropertyToID("_CharFadeWidth");
        private static readonly int charSlantAnglePropertyId = Shader.PropertyToID("_CharSlantAngle");

        private readonly TextRevealState revealState = new TextRevealState();
        private Transform canvasTransformCache;
        private Vector3[] worldCorners = new Vector3[4];
        private Vector3[] canvasCorners = new Vector3[4];
        private Vector4 curLineClipRect, curCharClipRect;
        private float curCharFadeWidth, curCharSlantAngle;
        private CharInfo revealStartChar = CharInfo.Invalid;
        private float lastRevealDuration, lastRevealTime, lastCharClipRectX, lastCharFadeWidth;
        private bool rebuildPending;
        private float revealAfterRebuild = -1;
        private Vector3 positionLastFrame;

        public virtual void RevealNextChars (int count, float duration, CancellationToken cancellationToken)
        {
            revealState.Start(count, duration, cancellationToken);
        }

        public virtual Vector2 GetLastRevealedCharPosition ()
        {
            UpdateClipRects();
            var lastChar = GetVisibleCharAt(LastRevealedVisibleCharIndex);
            var localPos = new Vector2(curCharClipRect.x, curCharClipRect.w - lastChar.Line.height / pixelsPerUnit);
            return CanvasTransform.TransformPoint(localPos);
        }

        public virtual char GetLastRevealedChar ()
        {
            var absIndex = VisibleToAbsoluteCharIndex(LastRevealedVisibleCharIndex);
            if (Text is null || absIndex < 0 || absIndex >= Text.Length)
                return default;
            return Text[absIndex];
        }

        public override void Rebuild (CanvasUpdate update)
        {
            base.Rebuild(update);

            // Visible char indexes could potentially change after the rebuild; recalculate them.
            LastVisibleCharIndex = FindLastVisibleCharIndex();
            // Set current last revealed char as the start position for the reveal effect to 
            // prevent it from affecting this char again when resuming the revealing without resetting the text.
            if (RevealProgress == 0) // Prevent flickering when starting to reveal first line.
                revealStartChar = CharInfo.Invalid;
            else revealStartChar = GetVisibleCharAt(LastRevealedVisibleCharIndex);

            rebuildPending = false;

            if (revealAfterRebuild != -1)
            {
                SetRevealProgress(revealAfterRebuild);
                UpdateClipRects();
                revealAfterRebuild = -1;
            }
        }

        protected override void Start ()
        {
            base.Start();
            if (!Application.isPlaying) return; // Text : ... : Graphic has [ExecuteInEditMode]

            material = new Material(Shader.Find(textShaderName));
            positionLastFrame = transform.position;
        }

        protected override void OnRectTransformDimensionsChange ()
        {
            base.OnRectTransformDimensionsChange();
            if (!Application.isPlaying) return; // Text : ... : Graphic has [ExecuteInEditMode]

            // When text layout changes (eg, content size fitter decides to increase height),
            // we need to force-update clip rect; otherwise it will be delayed by one frame
            // and user fill see incorrectly revealed text for a moment.
            UpdateClipRects();
            Update();
        }

        private void Update ()
        {
            if (!Application.isPlaying) return; // TextMeshProUGUI has [ExecuteInEditMode]

            UpdateRevealState();

            if (slideClipRect)
            {
                var slidedCharClipRectX = Mathf.Lerp(lastCharClipRectX, curCharClipRect.x, SlideProgress);
                var slidedCharClipRect = new Vector4(slidedCharClipRectX, curCharClipRect.y, curCharClipRect.z, curCharClipRect.w);
                var slidedFadeWidth = Mathf.Lerp(lastCharFadeWidth, curCharFadeWidth, SlideProgress);
                SetMaterialProperties(curLineClipRect, slidedCharClipRect, slidedFadeWidth, curCharSlantAngle);
            }
            else SetMaterialProperties(curLineClipRect, curCharClipRect, curCharFadeWidth, curCharSlantAngle);

            //Debug.DrawLine(CanvasTransform.TransformPoint(new Vector3(curLineClipRect.x, curLineClipRect.y)), CanvasTransform.TransformPoint(new Vector3(curLineClipRect.z, curLineClipRect.w)), Color.green);
            //Debug.DrawLine(CanvasTransform.TransformPoint(new Vector3(curCharClipRect.x, curCharClipRect.y)), CanvasTransform.TransformPoint(new Vector3(curCharClipRect.z, curCharClipRect.w)), Color.yellow);
        }

        private void LateUpdate ()
        {
            if (transform.position != positionLastFrame)
            {
                UpdateClipRects();
                Update();
            }

            positionLastFrame = transform.position;
        }

        private void UpdateRevealState ()
        {
            if (!revealState.InProgress) return;

            if (LastRevealedVisibleCharIndex >= LastVisibleCharIndex)
            {
                revealState.Reset();
                return;
            }

            // While rebuild is pending, we can't rely on visible char indexes, so wait.
            while (rebuildPending && !revealState.CancellationToken.IsCancellationRequested) return;
            if (revealState.CancellationToken.IsCancellationRequested) { revealState.Reset(); return; }

            // Skip invisible characters (eg, formating tags).
            var nextVisibleCharIndex = FindNextVisibleCharIndex(LastRevealedVisibleCharIndex);
            if (nextVisibleCharIndex == -1) // No visible characters left to reveal.
            {
                RevealAll();
                return;
            }

            // Wait while the clip rects are slided over currently revealed character.
            if (slideClipRect && SlideProgress < 1 && !revealState.CancellationToken.IsCancellationRequested) return;
            if (revealState.CancellationToken.IsCancellationRequested) { revealState.Reset(); return; }

            if (revealState.CharactersRevealed == revealState.CharactersToReveal)
            {
                revealState.Reset();
                return;
            }

            lastRevealDuration = Mathf.Max(revealState.RevealDuration, 0);
            lastRevealTime = Time.time;

            SetLastRevealedVisibleCharIndex(nextVisibleCharIndex);

            revealState.CharactersRevealed++;
        }

        private void RevealAll ()
        {
            if (rebuildPending) revealAfterRebuild = 1f;
            else SetLastRevealedVisibleCharIndex(LastVisibleCharIndex);
            lastRevealDuration = 0f; // Force the slide to complete instantly.
            revealState.Reset();
        }

        private void HideAll ()
        {
            SetLastRevealedVisibleCharIndex(-1);
            lastRevealDuration = 0f; // Force the slide to complete instantly.
            revealStartChar = CharInfo.Invalid; // Invalidate the reveal start position.
            Update(); // Otherwise the unrevealed yet text could be visible for a moment.
            revealState.Reset();
        }

        private void SetMaterialProperties (Vector4 lineClipRect, Vector4 charClipRect, float charFadeWidth, float charSlantAngle)
        {
            material.SetVector(lineClipRectPropertyId, lineClipRect);
            material.SetVector(charClipRectPropertyId, charClipRect);
            material.SetFloat(charFadeWidthPropertyId, charFadeWidth);
            material.SetFloat(charSlantAnglePropertyId, charSlantAngle);
        }

        private void SetLastRevealedVisibleCharIndex (int visibleCharIndex)
        {
            if (LastRevealedVisibleCharIndex == visibleCharIndex) return;

            var curChar = GetVisibleCharAt(LastRevealedVisibleCharIndex);
            var nextChar = GetVisibleCharAt(visibleCharIndex);

            lastCharClipRectX = curChar.LineIndex < 0 ? curLineClipRect.x : curCharClipRect.x;
            lastCharFadeWidth = curCharFadeWidth;

            LastRevealedVisibleCharIndex = visibleCharIndex;
            UpdateClipRects();

            // Reset the slide when switching lines.
            if (slideClipRect && curChar.LineIndex != nextChar.LineIndex)
            {
                lastCharClipRectX = GetTextCornersInCanvasSpace().x;
                lastCharFadeWidth = curCharFadeWidth;
            }
        }

        private float GetRevealProgress ()
        {
            var result = 0f;
            if (LastVisibleCharIndex <= 0) result = LastRevealedVisibleCharIndex >= 0 ? 1f : 0f;
            result = Mathf.Clamp01(LastRevealedVisibleCharIndex / (float)LastVisibleCharIndex);
            if (rebuildPending) result = Mathf.Clamp(result, 0, .999f);
            return result;
        }

        private void SetRevealProgress (float revealProgress)
        {
            if (revealProgress >= 1) { RevealAll(); return; }
            else if (revealProgress <= 0) { HideAll(); return; }

            if (rebuildPending)
            {
                revealAfterRebuild = revealProgress;
                return;
            }

            var charIndex = Mathf.CeilToInt(LastVisibleCharIndex * revealProgress);
            SetLastRevealedVisibleCharIndex(charIndex);
        }

        private void UpdateClipRects ()
        {
            if (LastRevealedVisibleCharIndex > LastVisibleCharIndex) return;

            var fullClipRect = GetTextCornersInCanvasSpace();

            if (LastRevealedVisibleCharIndex < 0) // Hide all.
            {
                curLineClipRect = fullClipRect;
                curCharClipRect = fullClipRect;
                return;
            }

            var currentChar = GetVisibleCharAt(LastRevealedVisibleCharIndex);
            var lineFirstChar = GetVisibleCharAt(AbsoluteToVisibleCharIndex(currentChar.Line.startCharIdx));
            var lineLastChar = GetLastVisibleCharAtLine(currentChar.Line.startCharIdx, currentChar.LineIndex);

            var lineTopY = currentChar.Ascender + (rectTransform.pivot.y - 1f) * cachedTextGenerator.rectExtents.height;
            var lineBottomY = lineTopY - currentChar.Line.height;
            var clipPosX = currentChar.XAdvance + rectTransform.pivot.x * cachedTextGenerator.rectExtents.width;

            curLineClipRect = fullClipRect + new Vector4(0, 0, 0, lineBottomY / pixelsPerUnit);
            curCharClipRect = fullClipRect + new Vector4(clipPosX / pixelsPerUnit, 0, 0, lineTopY / pixelsPerUnit);
            curCharClipRect.y = curLineClipRect.w;

            // We need to limit the fade width, so that it doesn't stretch before the first (startLimit) and last (endLimit) chars in the line.
            // Additionally, we need to handle cases when appending text, so that last revealed char won't get hidden again when resuming (revealStartChar is used instead of lineFirstChar).
            var startLimit = currentChar.LineIndex == revealStartChar.LineIndex ? currentChar.Origin - revealStartChar.Origin : currentChar.XAdvance - lineFirstChar.Origin;
            var endLimit = lineLastChar.XAdvance - currentChar.XAdvance;
            var widthLimit = Mathf.Min(startLimit, endLimit);
            curCharFadeWidth = Mathf.Clamp(revealFadeWidth, 0f, widthLimit);

            // TODO: Find a way to find the slant (italic) angle of the last revealed character.
            curCharSlantAngle = italicSlantAngle;
        }

        private Vector4 GetTextCornersInCanvasSpace ()
        {
            rectTransform.GetWorldCorners(worldCorners);
            for (int i = 0; i < 4; i++)
                canvasCorners[i] = CanvasTransform.InverseTransformPoint(worldCorners[i]);

            // Positions of diagonal corners.
            return new Vector4(canvasCorners[0].x, canvasCorners[0].y, canvasCorners[2].x, canvasCorners[2].y);
        }

        private CharInfo GetVisibleCharAt (int requestedVisibleCharIndex)
        {
            var absoluteIndex = VisibleToAbsoluteCharIndex(requestedVisibleCharIndex);
            if (absoluteIndex < 0 || absoluteIndex >= cachedTextGenerator.characterCount)
                return CharInfo.Invalid;

            var lineInfo = FindLineContainingChar(absoluteIndex, out var lineIndex);
            var visibleCharInfo = cachedTextGenerator.characters[absoluteIndex];
            return new CharInfo(requestedVisibleCharIndex, lineIndex, visibleCharInfo, lineInfo);
        }

        private CharInfo GetLastVisibleCharAtLine (int firstAbsoluteCharInLineIndex, int lineIndex)
        {
            var curVisibleCharIndex = -1;
            var resultIndex = -1;
            for (var i = 0; i < cachedTextGenerator.characterCount; i++)
            {
                if (cachedTextGenerator.characters[i].charWidth > 0)
                    curVisibleCharIndex++;
                if (i < firstAbsoluteCharInLineIndex) continue;

                FindLineContainingChar(i, out var curLindeIndex);
                if (lineIndex < curLindeIndex) break;

                resultIndex = curVisibleCharIndex;
            }
            return GetVisibleCharAt(resultIndex);
        }

        private UILineInfo FindLineContainingChar (int absoluteCharIndex, out int lineIndex)
        {
            lineIndex = 0;
            for (int i = 0; i < cachedTextGenerator.lineCount; i++)
            {
                if (cachedTextGenerator.lines[i].startCharIdx > absoluteCharIndex)
                    break;
                lineIndex = i;
            }
            return cachedTextGenerator.lines[lineIndex];
        }

        private int FindNextVisibleCharIndex (int startVisibleCharIndex = 0)
        {
            var curVisibleIndex = -1;
            for (int i = 0; i < cachedTextGenerator.characterCount; i++)
            {
                if (cachedTextGenerator.characters[i].charWidth == 0f) continue;
                curVisibleIndex++;
                if (curVisibleIndex <= startVisibleCharIndex) continue;
                return curVisibleIndex;
            }
            return -1;
        }

        private int FindLastVisibleCharIndex ()
        {
            var curVisibleIndex = -1;
            for (int i = 0; i < cachedTextGenerator.characterCount; i++)
            {
                if (cachedTextGenerator.characters[i].charWidth == 0f) continue;
                curVisibleIndex++;
            }
            return curVisibleIndex;
        }

        private int AbsoluteToVisibleCharIndex (int absoluteCharIndex)
        {
            var curVisibleIndex = -1;
            for (int i = 0; i < cachedTextGenerator.characterCount; i++)
            {
                if (cachedTextGenerator.characters[i].charWidth == 0f) continue;
                curVisibleIndex++;
                if (i >= absoluteCharIndex) break;
            }
            return curVisibleIndex;
        }

        private int VisibleToAbsoluteCharIndex (int visibleCharIndex)
        {
            var curVisibleIndex = -1;
            for (int i = 0; i < cachedTextGenerator.characterCount; i++)
            {
                if (cachedTextGenerator.characters[i].charWidth == 0f) continue;
                curVisibleIndex++;
                if (curVisibleIndex >= visibleCharIndex) return i;
            }
            return -1;
        }
    }
}
