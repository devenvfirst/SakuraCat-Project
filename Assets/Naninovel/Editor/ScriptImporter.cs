// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.IO;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Naninovel
{
    [ScriptedImporter(version: 20, ext: "nani")]
    public class ScriptImporter : ScriptedImporter
    {
        public override void OnImportAsset (AssetImportContext ctx)
        {
            var contents = string.Empty;

            try
            {
                var bytes = File.ReadAllBytes(ctx.assetPath);
                contents = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                // Purge BOM. Unity auto adding it when creating script assets: https://git.io/fjVgY
                if (contents.Length > 0 && contents[0] == '\uFEFF')
                {
                    contents = contents.Substring(1);
                    File.WriteAllText(ctx.assetPath, contents);
                }
            }
            catch (IOException exc)
            {
                ctx.LogImportError($"IOException: {exc.Message}");
            }
            finally
            {
                var assetName = Path.GetFileNameWithoutExtension(ctx.assetPath); ;
                var asset = Script.FromScriptText(assetName, contents);
                asset.hideFlags = HideFlags.NotEditable;

                ctx.AddObjectToAsset("naniscript", asset);
                ctx.SetMainObject(asset);
            }
        }
    }
}
