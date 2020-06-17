// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Naninovel
{
    public class LocalizationWindow : EditorWindow
    {
        protected string SourceScriptsPath { get => PlayerPrefs.GetString(sourceScriptsPathKey); set { PlayerPrefs.SetString(sourceScriptsPathKey, value); ValidateOutputPath(); } }
        protected string SourceManagedTextPath 
        { 
            get => PlayerPrefs.GetString(sourceManagedTextPathKey, $"{Application.dataPath}/Resources/Naninovel/{ProjectConfigurationProvider.LoadOrDefault<ManagedTextConfiguration>().Loader.PathPrefix}"); 
            set => PlayerPrefs.SetString(sourceManagedTextPathKey, value); 
        }
        protected string LocaleFolderPath { get => PlayerPrefs.GetString(localeFolderPathKey); set { PlayerPrefs.SetString(localeFolderPathKey, value); ValidateOutputPath(); } }

        private const string sourceScriptsPathKey = "Naninovel." + nameof(LocalizationWindow) + "." + nameof(SourceScriptsPath);
        private const string sourceManagedTextPathKey = "Naninovel." + nameof(LocalizationWindow) + "." + nameof(SourceManagedTextPath);
        private const string localeFolderPathKey = "Naninovel." + nameof(LocalizationWindow) + "." + nameof(LocaleFolderPath);
        private const string progressBarTitle = "Generating Localization Resources";

        private static readonly GUIContent localeFolderPathContent = new GUIContent("Locale Folder (output)", "The folder for the target locale where to store generated localization resources. Should be inside localization root (`Assets/Resources/Naninovel/Localization` by default) and have a name equal to one of the supported localization tags.");
        private static readonly GUIContent sourceScriptsPathContent = new GUIContent("Script Folder (input)", "When points to a folder with a previously generated script localization documents, will extract the source text to translate from them instead of the original (source locale) scripts.");
        private static readonly GUIContent sourceManagedTextPathContent = new GUIContent("Text Folder (input)", "Folder under which the source managed text documents are stored (`Resources/Naninovel/Text` by default).");
        private static readonly GUIContent localizeManagedTextContent = new GUIContent("Localize Mananged Text", "Whether to also generate localization documents for the managed text.");
        private static readonly GUIContent tryUpdateContent = new GUIContent("Try Update", "Whether to preserve existing trasnlation for the lines that didn't change.");
        private static readonly GUIContent autoTranslateContent = new GUIContent("Auto Translate", "Whether to provide Google Translate machine translation for the missing lines. Command lines and injected expressions won't be affected.\n\nBe aware, that public Google Translate web API limits request frequency per IP and won't process too much text at a time; the service could also sometimes fail to translate particular text causing warnings during the process.");

        private static readonly Regex CaptureInlinedRegex = new Regex(@"(?<!\\)\[(.*)(?<!\\)\]"); // The same as DynamicValueData.CaptureExprRegex, but for square brackets.
        private static readonly Regex CaptureTagsRegex = new Regex(@"(?<!\\)\<(.*)(?<!\\)\>"); // The same as DynamicValueData.CaptureExprRegex, but for angle brackets.

        private LocalizationConfiguration config;
        private bool tryUpdate = true, localizeManagedText = true, autoTranslate = false;
        private int wordCount = -1;
        private bool outputPathValid = false, scriptSourcePathValid = false;
        private string targetTag, targetLanguage, sourceTag, sourceLanguge;

        [MenuItem("Naninovel/Tools/Localization")]
        public static void OpenWindow ()
        {
            var position = new Rect(100, 100, 500, 300);
            GetWindowWithRect<LocalizationWindow>(position, true, "Localization", true);
        }

        private void OnEnable ()
        {
            config = ProjectConfigurationProvider.LoadOrDefault<LocalizationConfiguration>();
            ValidateOutputPath();
        }

        private void ValidateOutputPath ()
        {
            var localizationRoot = config.Loader.PathPrefix;
            targetTag = LocaleFolderPath?.GetAfter("/");
            sourceTag = SourceScriptsPath?.GetAfterFirst($"{localizationRoot}/")?.GetBefore("/");
            outputPathValid = LocaleFolderPath?.GetBeforeLast("/")?.EndsWith(localizationRoot) ?? false && 
                LanguageTags.ContainsTag(targetTag) && targetTag != config.SourceLocale;
            scriptSourcePathValid = LanguageTags.ContainsTag(sourceTag) && targetTag != sourceTag && sourceTag != config.SourceLocale;
            if (!scriptSourcePathValid) sourceTag = config.SourceLocale;
            if (outputPathValid)
            {
                targetLanguage = LanguageTags.GetLanguageByTag(targetTag);
                sourceLanguge = scriptSourcePathValid ? LanguageTags.GetLanguageByTag(sourceTag) : $"{LanguageTags.GetLanguageByTag(config.SourceLocale)} (source)";
            }
        }

        private void OnGUI ()
        {
            EditorGUILayout.LabelField("Naninovel Localization", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("The tool to generate localization resources; see `Localization` guide for usage instructions.", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                LocaleFolderPath = EditorGUILayout.TextField(localeFolderPathContent, LocaleFolderPath);
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(65)))
                    LocaleFolderPath = EditorUtility.OpenFolderPanel("Locale Folder Path", "", "");
            }
            if (outputPathValid)
                EditorGUILayout.HelpBox(targetLanguage, MessageType.None, false);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                SourceScriptsPath = EditorGUILayout.TextField(sourceScriptsPathContent, SourceScriptsPath);
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(65)))
                    SourceScriptsPath = EditorUtility.OpenFolderPanel("Locale Folder Path", "", "");
            }
            if (outputPathValid)
                EditorGUILayout.HelpBox(sourceLanguge, MessageType.None, false);

            EditorGUILayout.Space();

            if (localizeManagedText)
                using (new EditorGUILayout.HorizontalScope())
                {
                    SourceManagedTextPath = EditorGUILayout.TextField(sourceManagedTextPathContent, SourceManagedTextPath);
                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(65)))
                        SourceManagedTextPath = EditorUtility.OpenFolderPanel("Locale Folder Path", "", "");
                }

            EditorGUILayout.Space();

            localizeManagedText = EditorGUILayout.Toggle(localizeManagedTextContent, localizeManagedText);
            tryUpdate = EditorGUILayout.Toggle(tryUpdateContent, tryUpdate);
            //autoTranslate = EditorGUILayout.Toggle(autoTranslateContent, autoTranslate);
            GUILayout.FlexibleSpace();

            EditorGUILayout.HelpBox(wordCount >= 0 ? $"Total word count in the localization documents: {wordCount}." : 
                "Total word count in the localization documents will be printed here after the documents are generated.", MessageType.Info);

            if (!outputPathValid)
                if (targetTag == config.SourceLocale) EditorGUILayout.HelpBox($"You're trying to create a `{targetTag}` localization, which is equal to the project source locale. That is not allowed; see `Localization` guide for more info.", MessageType.Error);
                else EditorGUILayout.HelpBox("Locale Folder path is not valid. Make sure it points to a folder inside localization root with name equal to one of the supported language tags.", MessageType.Error);

            EditorGUI.BeginDisabledGroup(!outputPathValid);
            if (GUILayout.Button("Generate Localization Resources", GUIStyles.NavigationButton))
                GenerateLocalizationResources();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
        }

        private async void GenerateLocalizationResources ()
        {
            EditorUtility.DisplayProgressBar(progressBarTitle, "Reading source documents...", 0f);

            try
            {
                await LocalizeScriptsAsync();
                if (localizeManagedText) await LocalizeManagedTextAsync();

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to generate localization resources: {e.Message}");
            }

            EditorUtility.ClearProgressBar();
            Repaint();
        }

        private async UniTask LocalizeScriptsAsync ()
        {
            wordCount = 0;

            var scriptPathPrefix = ProjectConfigurationProvider.LoadOrDefault<ScriptsConfiguration>().Loader.PathPrefix;
            var outputDirPath = $"{LocaleFolderPath}/{scriptPathPrefix}";
            if (!Directory.Exists(outputDirPath))
                Directory.CreateDirectory(outputDirPath);

            if (scriptSourcePathValid) // Generate based on already generated docs for an other language.
            {
                var sourceScriptPaths = Directory.GetFiles(SourceScriptsPath, "*.nani", SearchOption.AllDirectories);
                for (int pathIdx = 0; pathIdx < sourceScriptPaths.Length; pathIdx++)
                {
                    var sourceScriptPath = sourceScriptPaths[pathIdx];
                    var sourceScriptName = Path.GetFileNameWithoutExtension(sourceScriptPath);
                    var progress = pathIdx / (float)sourceScriptPaths.Length;

                    EditorUtility.DisplayProgressBar(progressBarTitle, $"Processing `{sourceScriptName}`...", progress);

                    var sourceScript = AssetDatabase.LoadAssetAtPath<Script>(PathUtils.AbsoluteToAssetPath(sourceScriptPath));
                    var sourceTextLines = Script.SplitScriptText(File.ReadAllText(sourceScriptPath));
                    var outputPath = sourceScriptPath.Replace(LanguageTags.GetTagByLanguage(sourceLanguge), LanguageTags.GetTagByLanguage(targetLanguage));
                    var outputBuilder = new StringBuilder($"{CommentScriptLine.IdentifierLiteral} Localization script for `{sourceScriptName}`\n");

                    var existingScript = tryUpdate ? AssetDatabase.LoadAssetAtPath<Script>(PathUtils.AbsoluteToAssetPath(outputPath)) : null;
                    var existingTextLines = existingScript ? Script.SplitScriptText(File.ReadAllText(outputPath)) : null;
                    var autoTranslatedTextLines = autoTranslate ? await AutoTranslate(sourceTag, targetTag, sourceTextLines, sourceScriptName, progress) : null;

                    Debug.Assert(sourceScript.Lines.Count == sourceTextLines.Length);
                    Debug.Assert(!existingScript || existingScript.Lines.Count == existingTextLines.Length);
                    Debug.Assert(autoTranslatedTextLines is null || autoTranslatedTextLines.Length == sourceTextLines.Length);

                    var currentLabelText = default(string);
                    var currentSourceLine = default(string);
                    var currentAutoTranslateLines = new List<string>();
                    for (int lineIdx = 1; lineIdx < sourceScript.Lines.Count; lineIdx++) // Starting from one to skip title comment.
                    {
                        var sourceLine = sourceScript.Lines[lineIdx];
                        if (sourceLine is LabelScriptLine labelLine)
                        {
                            var locIdx = (existingScript && currentLabelText != null) ? existingScript.GetLineIndexForLabel(currentLabelText) : -1;
                            var appendedAnyExistinglines = false;
                            if (locIdx > -1) // Existing localization value is still valid, preserve it.
                            {
                                while (existingScript.Lines.IsIndexValid(locIdx + 1))
                                {
                                    locIdx++;
                                    var existingLine = existingScript.Lines[locIdx];
                                    if (existingLine is CommentScriptLine) continue;
                                    if (existingLine is LabelScriptLine) break;
                                    outputBuilder.AppendLine(existingTextLines[locIdx]);
                                    appendedAnyExistinglines = true;
                                }
                            }

                            if (!appendedAnyExistinglines && currentAutoTranslateLines.Count > 0)
                                foreach (var line in currentAutoTranslateLines)
                                    outputBuilder.AppendLine(line);
                            currentAutoTranslateLines.Clear();

                            currentLabelText = labelLine.LabelText;
                            outputBuilder.AppendLine();
                            outputBuilder.AppendLine($"{LabelScriptLine.IdentifierLiteral} {labelLine.LabelText}");
                            continue;
                        }
                        if (!IsLineLocalizable(sourceLine)) continue; // Whether the line contains some actual translation.

                        currentSourceLine = sourceTextLines[lineIdx];
                        outputBuilder.AppendLine($"{CommentScriptLine.IdentifierLiteral} {currentSourceLine}"); // Copy the source text to comments.

                        if (autoTranslatedTextLines != null && !string.IsNullOrEmpty(autoTranslatedTextLines[lineIdx]))
                            currentAutoTranslateLines.Add(autoTranslatedTextLines[lineIdx]);

                        CountWords(sourceTextLines[lineIdx]);
                    }

                    File.WriteAllText(outputPath, outputBuilder.ToString(), Encoding.UTF8);
                }
            }
            else // Generate based on source scripts.
            {
                var sourceScriptPaths = EditorResources.LoadOrDefault().GetAllRecords(scriptPathPrefix).Select(kv => AssetDatabase.GUIDToAssetPath(kv.Value)).ToArray();
                for (int pathIdx = 0; pathIdx < sourceScriptPaths.Length; pathIdx++)
                {
                    var sourceScriptPath = sourceScriptPaths[pathIdx];
                    if (!File.Exists(sourceScriptPath)) continue;
                    var sourceScriptName = Path.GetFileNameWithoutExtension(sourceScriptPath);
                    var progress = pathIdx / (float)sourceScriptPaths.Length;

                    EditorUtility.DisplayProgressBar(progressBarTitle, $"Processing `{sourceScriptName}`...", pathIdx / (float)sourceScriptPaths.Length);

                    var sourceText = File.ReadAllText(sourceScriptPath);
                    var sourceTextLines = Script.SplitScriptText(sourceText);
                    var sourceScript = AssetDatabase.LoadAssetAtPath<Script>(sourceScriptPath);
                    var outputPath = $"{outputDirPath}/{sourceScript.Name}.nani";
                    var outputBuilder = new StringBuilder($"{CommentScriptLine.IdentifierLiteral} Localization script for `{sourceScript.Name}`\n");

                    var existingScript = tryUpdate ? AssetDatabase.LoadAssetAtPath<Script>(PathUtils.AbsoluteToAssetPath(outputPath)) : null;
                    var existingTextLines = existingScript ? Script.SplitScriptText(File.ReadAllText(outputPath)) : null;
                    var autoTranslatedTextLines = autoTranslate ? await AutoTranslate(sourceTag, targetTag, sourceTextLines, sourceScriptName, progress) : null;

                    Debug.Assert(sourceScript.Lines.Count == sourceTextLines.Length);
                    Debug.Assert(!existingScript || existingScript.Lines.Count == existingTextLines.Length);
                    Debug.Assert(autoTranslatedTextLines is null || autoTranslatedTextLines.Length == sourceTextLines.Length);

                    for (int lineIdx = 0; lineIdx < sourceScript.Lines.Count; lineIdx++)
                    {
                        var sourceLine = sourceScript.Lines[lineIdx];
                        if (!IsLineLocalizable(sourceLine)) continue;

                        var sourceTextLine = sourceTextLines[lineIdx];

                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine($"{LabelScriptLine.IdentifierLiteral} {sourceLine.LineHash}");
                        outputBuilder.AppendLine($"{CommentScriptLine.IdentifierLiteral} {sourceTextLine}");

                        var locIdx = existingScript ? existingScript.GetLineIndexForLabel(sourceLine.LineHash) : -1;
                        var appendedAnyExistinglines = false;
                        if (locIdx > -1) // Existing localization value is still valid, preserve it.
                        {
                            while (existingScript.Lines.IsIndexValid(locIdx + 1))
                            {
                                locIdx++;
                                var existingLine = existingScript.Lines[locIdx];
                                if (existingLine is CommentScriptLine) continue;
                                if (existingLine is LabelScriptLine) break;
                                outputBuilder.AppendLine(existingTextLines[locIdx]);
                                appendedAnyExistinglines = true;
                            }
                        }

                        if (!appendedAnyExistinglines && autoTranslatedTextLines != null && !string.IsNullOrEmpty(autoTranslatedTextLines[lineIdx]))
                            outputBuilder.AppendLine(autoTranslatedTextLines[lineIdx]);

                        CountWords(sourceTextLine);
                    }

                    File.WriteAllText(outputPath, outputBuilder.ToString(), Encoding.UTF8);
                }
            }

            EditorUtility.ClearProgressBar();

            bool IsLineLocalizable (ScriptLine line)
            {
                if (line is GenericTextScriptLine genericLine)
                    return genericLine.InlinedCommands.Any(c => c is Command.ILocalizable);
                if (line is CommandScriptLine commandLine)
                    return commandLine.Command is Command.ILocalizable;
                return false;
            }

            // string.Split(null) will delimit by whitespace chars; `default(char[])` is used to prevent ambiguity in case of overloads.
            void CountWords (string value) => wordCount += value.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private async UniTask LocalizeManagedTextAsync ()
        {
            if (!Directory.Exists(SourceManagedTextPath)) return;

            var outputPath = $"{LocaleFolderPath}/{ProjectConfigurationProvider.LoadOrDefault<ManagedTextConfiguration>().Loader.PathPrefix}";
            if (!Directory.Exists(outputPath)) 
                Directory.CreateDirectory(outputPath);

            var filePaths = Directory.GetFiles(SourceManagedTextPath, "*.txt", SearchOption.AllDirectories);
            for (int i = 0; i < filePaths.Length; i++)
            {
                var docPath = filePaths[i];
                var docText = File.ReadAllText(docPath);
                var category = Path.GetFileNameWithoutExtension(docPath);
                var targetPath = Path.Combine(outputPath, $"{category}.txt");
                var records = ManagedTextUtils.ParseDocument(docText, category);

                if (tryUpdate && File.Exists(targetPath))
                {
                    var existingText = File.ReadAllText(targetPath);
                    var existingRecords = ManagedTextUtils.ParseDocument(existingText, category);

                    foreach (var existingRecord in existingRecords)
                    {
                        if (!records.Remove(existingRecord)) continue;
                        records.Add(existingRecord);
                    }
                }
                else if (autoTranslate)
                {
                    var sourceTextLines = records.Select(r => r.ToDocumentTextLine()).ToArray();
                    var translatedTextLines = await AutoTranslate(sourceTag, targetTag, sourceTextLines, category, i / (float)filePaths.Length);
                    if (translatedTextLines is null)
                    {
                        Debug.LogWarning($"Failed to auto-translate managed text documents. Disable auto-translate and try again.");
                        return;
                    }
                    File.WriteAllLines(targetPath, translatedTextLines, Encoding.UTF8);
                    continue;
                }

                var outputBuilder = new StringBuilder();
                foreach (var record in records)
                    outputBuilder.AppendLine(record.ToDocumentTextLine());
                File.WriteAllText(targetPath, outputBuilder.ToString(), Encoding.UTF8);
            }
        }

        private static async UniTask<string[]> AutoTranslate (string sourceLang, string targetLang, string[] sourceTextLines, string sourceDocumentName, float progress)
        {
            EditorUtility.DisplayProgressBar(progressBarTitle, $"Google Translate `{sourceDocumentName}`...", progress);

            const string lineSeparator = "3df75c9f8";
            const string injectLiteral = "1i4a028c2";
            const string injectPointer = "||";

            var injectList = new List<string>();
            var requestLines = new List<string>();
            for (int i = 0; i < sourceTextLines.Length; i++)
            {
                var lineText = sourceTextLines[i];
                var lineType = Script.ResolveLineType(lineText.TrimFull());
                if (lineType != typeof(GenericTextScriptLine) || string.IsNullOrWhiteSpace(lineText)) continue;

                // Capture author ID.
                var authorId = lineText.GetBefore(GenericTextScriptLine.AuthorIdLiteral);
                if (!string.IsNullOrEmpty(authorId) && !authorId.Any(char.IsWhiteSpace) && !authorId.StartsWithFast("\""))
                {
                    lineText = $"{injectLiteral}{injectList.Count}{injectPointer}{lineText.GetAfterFirst(GenericTextScriptLine.AuthorIdLiteral)}";
                    injectList.Add(authorId + GenericTextScriptLine.AuthorIdLiteral);
                }

                // Capture injected expressions.
                foreach (Match match in DynamicValueData.CaptureExprRegex.Matches(lineText))
                {
                    lineText = lineText.Replace(match.Value, $"{injectLiteral}{injectList.Count}{injectPointer}");
                    injectList.Add(match.Value);
                }

                // Capture inlined commands.
                foreach (Match match in CaptureInlinedRegex.Matches(lineText))
                {
                    lineText = lineText.Replace(match.Value, $"{injectLiteral}{injectList.Count}{injectPointer}");
                    injectList.Add(match.Value);
                }

                // Capture html tags (text formattings).
                foreach (Match match in CaptureTagsRegex.Matches(lineText))
                {
                    lineText = lineText.Replace(match.Value, $"{injectLiteral}{injectList.Count}{injectPointer}");
                    injectList.Add(match.Value);
                }

                requestLines.Add($"{i}{injectPointer}{lineText}");
            }

            var requestText = UnityWebRequest.EscapeURL(string.Join(lineSeparator, requestLines), Encoding.UTF8);
            var uri = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t&q={requestText}";
            var request = UnityWebRequest.Get(uri);

            await request.SendWebRequest();

            if (request is null || request.isHttpError || request.isNetworkError || string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                Debug.LogWarning($"Failed to fetch Google Translate: {request?.error}");
                request.Dispose();
                return null;
            }

            var sanitizedResponse = SanitizeResponse(request.downloadHandler.text);

            var result = new string[sourceTextLines.Length];
            foreach (var responseLine in sanitizedResponse.Split(new[] { lineSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                // Extract source line index.
                if (!int.TryParse(responseLine.GetBefore(injectPointer), out var sourceLineIdx))
                {
                    Debug.LogWarning($"Failed to process google translation line `{responseLine}`.");
                    continue;
                }
                var translatedLine = responseLine.GetAfterFirst(injectPointer);

                // Handle injected parts.
                while (translatedLine.Contains(injectLiteral))
                {
                    var after = translatedLine.GetAfterFirst(injectLiteral);
                    var before = translatedLine.GetBefore(injectLiteral);
                    var idxString = after.GetBefore(injectPointer);
                    if (!int.TryParse(idxString, out var injectIdx))
                    {
                        Debug.LogWarning($"Failed to process google translation line `{responseLine}`.");
                        break;
                    }
                    translatedLine = before + injectList[injectIdx] + after.GetAfterFirst(injectPointer);
                }

                // Un-escape double quotes (Google Translate escapes them in response).
                translatedLine = translatedLine.Replace("\\\"", "\"");

                result[sourceLineIdx] = translatedLine;
            }
            request.Dispose();

            return result.ToArray();

            string SanitizeResponse (string response)
            {
                // https://i.gyazo.com/c7a032372831177e8a8c1f7a05b8b70f.png

                response = DecodeNonAsciiCharacters(response);
                var builder = new StringBuilder();
                var bracketLevel = 0;
                var insideBody = false;
                var insideSourceBody = false;
                for (int i = 0; i < response.Length; i++)
                {
                    var curChar = response[i];
                    var prevChar = i > 0 ? response[i - 1] : default;
                    var prePrevChar = i > 1 ? response[i - 2] : default;
                    var nextChar = (i + 1) < response.Length ? response[i + 1] : default;
                    var escaped = prevChar == '\\' && (i < 2 || prePrevChar != '\\');

                    if (!insideBody && !insideSourceBody)
                    {
                        if (curChar == '[') { bracketLevel++; continue; }
                        if (curChar == ']') { bracketLevel--; continue; }
                        if (bracketLevel == 3 && curChar == '"' && prevChar == '[') { insideBody = true; continue; }
                        continue;
                    }

                    if (insideBody && !escaped && curChar == '"' && nextChar == ',') { insideBody = false; insideSourceBody = true; continue; }
                    if (insideSourceBody && !escaped && curChar == '"' && nextChar == ',') { insideSourceBody = false; continue; }

                    if (!insideBody) continue;
                    if (curChar == ' ' && prevChar == injectPointer[1] && prePrevChar == injectPointer[0]) continue;
                    builder.Append(curChar);
                }

                return builder.ToString();

                string DecodeNonAsciiCharacters (string value) => Regex.Replace(value, @"\s?\\u(?<Value>[a-zA-Z0-9]{4})\s?",
                    m => ((char)int.Parse(m.Groups["Value"].Value, System.Globalization.NumberStyles.HexNumber)).ToString());
            }

        }
    }
}
