using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unifind.Internal;
using UnityEditor;
using UnityEngine;

namespace Unifind
{
    public class FuzzyFinderWindow : EditorWindow
    {
        const string FindSearchFieldCtrlName = "FindEditorToolsSearchField";
        const float ButtonStartPosition = 24.0f;
        const float ButtonHeight = 16.0f;
        const float ScrollbarWidth = 15.0f;
        const float IconWidth = 20.0f;
        const float SummaryRatio = 40.0f;

        static readonly Color HoverBgColor =
            (EditorGUIUtility.isProSkin)
                ? new Color32(38, 79, 120, 255)
                : new Color32(153, 201, 239, 255);

        static readonly Color DefaultBgColor =
            (EditorGUIUtility.isProSkin)
                ? new Color32(46, 46, 46, 255)
                : new Color32(180, 180, 180, 255);

        static readonly Color DefaultTextColor =
            (EditorGUIUtility.isProSkin)
                ? new Color(46, 46, 46)
                : // #2E2E2E
                new Color(0, 0, 0);

        // Yes - unfortunately we really do need to mark every private field as NonSerialized
        // to avoid unity only partially restoring state

        [NonSerialized]
        GUIStyle _guiStyleHover = new GUIStyle();

        [NonSerialized]
        GUIStyle _guiStyleDefault = new GUIStyle();

        // Dynamic state:
        [NonSerialized]
        bool _isOpen;

        [NonSerialized]
        bool _isDisposed;

        [NonSerialized]
        bool _hasResult;

        [NonSerialized]
        int _selectedIndex = 0;

        [NonSerialized]
        float _scrollBar = 0.0f;

        [NonSerialized]
        readonly List<FuzzyFinderEntry> _entries = new List<FuzzyFinderEntry>();

        [NonSerialized]
        readonly List<FuzzyFinderEntry> _entriesFiltered = new();

        [NonSerialized]
        string? _filter;

        [NonSerialized]
        TaskCompletionSource<FuzzyFinderEntry?>? _completionSource;

        [NonSerialized]
        GUIStyle? _errorTextStyle;

        [NonSerialized]
        bool _isDestroyed;

        GUIStyle ErrorTextStyle
        {
            get
            {
                if (_errorTextStyle == null)
                {
                    _errorTextStyle = new GUIStyle(GUI.skin.label);
                    _errorTextStyle.fontSize = 18;
                    _errorTextStyle.normal.textColor = Color.red;
                    _errorTextStyle.wordWrap = true;
                    _errorTextStyle.alignment = TextAnchor.MiddleCenter;
                }

                return _errorTextStyle;
            }
        }

        public void OnEnable()
        {
            _guiStyleHover.normal.textColor = DefaultTextColor;
            _guiStyleDefault.normal.textColor = DefaultTextColor;
        }

        public void OnDisable()
        {
            Dispose();
        }

        public void OnGUI()
        {
            try
            {
                HandleInput();
                DrawSearchBar();
                DrawSuggestions();

                if (EditorWindow.focusedWindow != this)
                {
                    CompleteWithResult(null);
                }
            }
            catch (Exception e)
            {
                Log.Error("Error during OnGUI: {0}", e);
                Dispose();
            }
        }

        void OnDestroy()
        {
            Assert.That(!_isDestroyed);
            _isDestroyed = true;
            Dispose();
        }

        void CompleteWithResult(FuzzyFinderEntry? value)
        {
            if (!_hasResult)
            {
                Assert.That(_isOpen);
                _hasResult = true;
                _completionSource!.SetResult(value);
            }
        }

        void SelectCurrentEntry()
        {
            FuzzyFinderEntry? choice;

            if (_entriesFiltered.Any())
            {
                choice = _entriesFiltered[_selectedIndex];
                Assert.That(choice != null);
                Log.Debug("Selected {0}", choice!.Name);
            }
            else
            {
                choice = null;
            }

            CompleteWithResult(choice);
        }

        void HandleInput()
        {
            var evt = Event.current;

            if (evt == null || evt.type != EventType.KeyDown)
            {
                return;
            }

            switch (evt.keyCode)
            {
                case KeyCode.KeypadEnter:
                case KeyCode.Return:
                {
                    evt.Use();
                    SelectCurrentEntry();
                    break;
                }
                case KeyCode.DownArrow:
                {
                    evt.Use();
                    ++_selectedIndex;

                    if (_selectedIndex >= _entriesFiltered.Count)
                    {
                        _selectedIndex = 0;
                    }

                    CheckScrollToSelected();
                    Repaint();
                    break;
                }
                case KeyCode.UpArrow:
                {
                    evt.Use();

                    --_selectedIndex;

                    if (_selectedIndex < 0)
                    {
                        _selectedIndex = _entriesFiltered.Count - 1;
                    }

                    CheckScrollToSelected();
                    Repaint();
                    break;
                }
                case KeyCode.Escape:
                {
                    evt.Use();
                    CompleteWithResult(null);
                    break;
                }
            }
        }

        public static async Task<T?> Select<T>(string title, IEnumerable<T> entries)
            where T : notnull
        {
            var window = EditorWindow.GetWindow<FuzzyFinderWindow>("Fuzzy Finder");
            var fuzzyEntries = entries
                .Select(e => new FuzzyFinderEntry<T>(
                    name: e.ToString(),
                    value: e
                ))
                .ToList();

            var result = await window.SelectImpl<T>(title, fuzzyEntries);

            if (result != null)
            {
                return result.Value;
            }

            return default;
        }

        public static Task<FuzzyFinderEntry<T>?> Select<T>(
            string title,
            List<FuzzyFinderEntry<T>> entries
        )
        {
            var window = EditorWindow.GetWindow<FuzzyFinderWindow>("Fuzzy Finder");
            return window.SelectImpl<T>(title, entries);
        }

        async Task<FuzzyFinderEntry<T>?> SelectImpl<T>(
            string title,
            List<FuzzyFinderEntry<T>> entries
        )
        {
            Assert.That(!_isOpen, "FuzzyFinderWindow is already open");
            _isOpen = true;

            Log.Debug("Opening FuzzyFinderWindow {0}", title);

            try
            {
                using (Log.SpanDebug("Running fuzzy finder {0}", title))
                {
                    var choice = await SelectImpl2(title, entries);

                    // Do not destroy window twice otherwise we get errors
                    // This can happen when user manually closes window
                    if (!_isDestroyed)
                    {
                        Close();
                    }

                    return (FuzzyFinderEntry<T>?)choice;
                }
            }
            finally
            {
                Log.Debug("Completed FuzzyFinderWindow {0}", title);
                _isOpen = false;
            }
        }

        void ResetSizeAndPosition()
        {
            Rect mainEditorWindowPos = EditorGUIUtility.GetMainWindowPosition();

            float windowWidth = 0.5f * mainEditorWindowPos.width;
            float windowHeight = 0.3f * mainEditorWindowPos.height;

            float centerX = mainEditorWindowPos.x + mainEditorWindowPos.width / 2;
            centerX -= windowWidth / 2;

            float marginTop = 200.0f;
            float centerY = mainEditorWindowPos.y + marginTop;

            position = new Rect(centerX, centerY, windowWidth, windowHeight);
        }

        Task<FuzzyFinderEntry?> SelectImpl2(string title, IEnumerable<FuzzyFinderEntry> entries)
        {
            Assert.That(_completionSource == null);
            _completionSource = new TaskCompletionSource<FuzzyFinderEntry?>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            // Reset state
            _hasResult = false;
            _entries.Clear();
            _entries.AddRange(entries);
            _selectedIndex = 0;
            _scrollBar = 0;
            _filter = "";

            titleContent = new GUIContent(title);
            ResetSizeAndPosition();
            UpdateFilteredEntries();

            return _completionSource.Task;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Log.Debug("Disposing FuzzyFinderWindow");

            if (_isOpen)
            {
                CompleteWithResult(null);
            }
        }

        void UpdateFilteredEntries()
        {
            _entriesFiltered.Clear();
            _selectedIndex = 0;

            if (_filter == null || _filter.Length == 0)
            {
                _entriesFiltered.AddRange(_entries);
            }
            else
            {
                foreach (var item in _entries)
                {
                    if (Util.IsSubsequenceMatch(item.Name, _filter!))
                    {
                        _entriesFiltered.Add(item);
                    }
                }
            }
        }

        void DrawSuggestions()
        {
            var winRect = new Rect(0, 0, position.width, position.height);
            float buttonAreaHeight = winRect.height - ButtonStartPosition;
            float buttonCount = buttonAreaHeight / ButtonHeight;
            int buttonCountFloor = Mathf.Min(Mathf.FloorToInt(buttonCount), _entriesFiltered.Count);
            int buttonCountCeil = Mathf.Min(Mathf.CeilToInt(buttonCount), _entriesFiltered.Count); // used to additionally show the last button (even if visible only in half)
            bool scrollbar = buttonCountCeil < _entriesFiltered.Count;

            if (scrollbar)
            {
                _scrollBar = GUI.VerticalScrollbar(
                    new Rect(
                        winRect.width - ScrollbarWidth,
                        ButtonStartPosition,
                        ScrollbarWidth,
                        winRect.height - ButtonStartPosition
                    ),
                    _scrollBar,
                    buttonCountFloor * ButtonHeight,
                    0.0f,
                    _entriesFiltered.Count * ButtonHeight
                );
            }

            int scrollBase = scrollbar ? Mathf.RoundToInt(_scrollBar / ButtonHeight) : 0;

            float summaryWidth = winRect.width * SummaryRatio / 100.0f;

            float scrollWidth = (scrollbar ? ScrollbarWidth : 0.0f);

            float width = winRect.width - scrollWidth - IconWidth;
            float height = ButtonHeight - 1.0f;

            float tooltipDisplayWidth = winRect.width - summaryWidth - scrollWidth;

            if (!_entriesFiltered.Any())
            {
                EditorGUI.LabelField(
                    new Rect(0, ButtonStartPosition, winRect.width, ButtonHeight),
                    "No results found",
                    _guiStyleDefault
                );
            }
            else
            {
                for (
                    int i = scrollBase,
                        j = 0,
                        imax = Mathf.Min(scrollBase + buttonCountCeil, _entriesFiltered.Count);
                    i < imax;
                    ++i, ++j
                )
                {
                    var entry = _entriesFiltered[i];
                    bool selected = (i == _selectedIndex);

                    float y = ButtonStartPosition + j * ButtonHeight;
                    var rectMain = new Rect(IconWidth, y, width, height);

                    EditorGUI.DrawRect(rectMain, selected ? HoverBgColor : DefaultBgColor);

                    float indentation = EditorGUI.IndentedRect(rectMain).x - rectMain.x;
                    _guiStyleHover.fixedWidth = rectMain.width - indentation;
                    _guiStyleDefault.fixedWidth = rectMain.width - indentation;

                    GUIStyle style = selected ? _guiStyleHover : _guiStyleDefault;
                    float nameWidth = new GUIStyle().CalcSize(new GUIContent(entry.Name)).x;
                    nameWidth += IconWidth;

                    float summaryStart = Mathf.Max(summaryWidth, nameWidth);

                    EditorGUI.LabelField(
                        rectMain,
                        new GUIContent(entry.Name, null, entry.Tooltip),
                        style
                    );

                    var rectIcon = new Rect(
                        0.0f,
                        ButtonStartPosition + j * ButtonHeight,
                        IconWidth,
                        ButtonHeight - 1.0f
                    );
                    EditorGUI.DrawRect(rectIcon, selected ? HoverBgColor : DefaultBgColor);

                    var iconTexture = string.IsNullOrEmpty(entry.Icon)
                        ? null
                        : EditorGuiHelper.TryFindTexture(entry.Icon!);
                    EditorGUI.LabelField(rectIcon, new GUIContent(iconTexture, entry.Tooltip));

                    var rectSummary = new Rect(summaryStart, y, tooltipDisplayWidth, height);
                    EditorGUI.LabelField(rectSummary, entry.Summary);
                }
            }
        }

        void DrawSearchBar()
        {
            void OnChangeOccurred()
            {
                UpdateFilteredEntries();
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _entriesFiltered.Count - 1);
                CheckScrollToSelected();
            }

            using (EditorGuiHelper.ChangeCheckBlock(OnChangeOccurred))
            using (EditorGuiHelper.HorizontalBlock())
            {
                var prompt = _entriesFiltered.Count + "/" + _entries.Count;

                float promptWidth = EditorStyles.label.CalcSize(new GUIContent(prompt)).x;
                EditorGUILayout.LabelField(prompt, GUILayout.Width(promptWidth));

                GUI.SetNextControlName(FindSearchFieldCtrlName);

                _filter = EditorGUILayout.TextField(
                    _filter,
                    GUI.skin.FindStyle("ToolbarSearchTextField")
                );

                if (String.IsNullOrEmpty(_filter) || _entriesFiltered.Count == 0)
                {
                    EditorGUI.FocusTextInControl(FindSearchFieldCtrlName);
                }
            }
        }

        void CheckScrollToSelected()
        {
            float buttonAreaHeight = position.height - ButtonStartPosition;
            float buttonCount = buttonAreaHeight / ButtonHeight;
            int scrollBase = Mathf.RoundToInt(_scrollBar / ButtonHeight);
            int buttonCountFloor = Mathf.Min(Mathf.FloorToInt(buttonCount), _entriesFiltered.Count);

            if (_selectedIndex >= Mathf.Min(scrollBase + buttonCountFloor, _entriesFiltered.Count))
            {
                _scrollBar = ButtonHeight * (_selectedIndex - buttonCountFloor + 1);
            }
            else if (_selectedIndex < scrollBase)
            {
                _scrollBar = ButtonHeight * _selectedIndex;
            }
        }
    }
}
