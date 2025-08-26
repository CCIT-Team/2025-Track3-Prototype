#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace KWS
{

    public class KWS_PipelineDefines : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets,
                                           string[] deletedAssets,
                                           string[] movedAssets,
                                           string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.Contains("KriptoFX/WaterSystem2"))
                {
                    //Debug.Log("KWS2 assets imported, applying keyword setup...");
                    UpdatePipelineDefine();
                    break;
                }
            }
        }
       
#if KWS_DEBUG
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RunOnStart()
        { 
           
            CheckPipelineChange();
        }
        
        static void CheckPipelineChange()
        {
            CheckAndUpdateShaderPipelineDefines();
             
        }
#endif
       
        private static RenderPipelineAsset _lastPipelineAsset;


        static void UpdatePipelineDefine()
        {
            var group = BuildTargetGroup.Standalone;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            defines = Remove(defines, "KWS_BUILTIN");
            defines = Remove(defines, "KWS_URP");
            defines = Remove(defines, "KWS_HDRP");

            KWS_EditorUtils.DisableAllShaderTextDefines(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile:true, "KWS_BUILTIN", "KWS_URP", "KWS_HDRP");

            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;

            if (pipeline == null)
            {
                defines += ";KWS_BUILTIN";
                KWS_EditorUtils.SetShaderTextDefine(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile: true, "KWS_BUILTIN", true);
                //Debug.Log("KWS2 pipeline changed to Built-in");
                
            }
            else
            {
                string rpName = pipeline.GetType().ToString();

                if (rpName.Contains("UniversalRenderPipelineAsset") || rpName.Contains("UniversalRenderPipeline"))
                {
                    defines += ";KWS_URP";
                    //Debug.Log("KWS2 pipeline changed to URP");
                    KWS_EditorUtils.SetShaderTextDefine(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile: true, "KWS_URP", true);
                }
                else if (rpName.Contains("HDRenderPipelineAsset"))
                {
                    defines += ";KWS_HDRP";
                    //Debug.Log("KWS2 pipeline changed to HDRP");
                    KWS_EditorUtils.SetShaderTextDefine(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile: true, "KWS_HDRP", true);
                }
                else
                {
                    Debug.LogError("KWS2 Water Unknown RenderPipeline: " + rpName);
                }
            }

            _lastPipelineAsset = pipeline;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);

           // CheckAndUpdateShaderPipelineDefines();
            AssetDatabase.Refresh();


        }

        static  void CheckAndUpdateShaderPipelineDefines()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null)
            {
                #if !KWS_BUILTIN
                    UpdatePipelineDefine();
                #endif
            }
            else
            {
                string rpName = pipeline.GetType().ToString();

                if (rpName.Contains("UniversalRenderPipelineAsset") || rpName.Contains("UniversalRenderPipeline"))
                {
#if !KWS_URP
                    UpdatePipelineDefine();
#endif
                }
                else if (rpName.Contains("HDRenderPipelineAsset"))
                {
#if !KWS_HDRP
                    UpdatePipelineDefine();
#endif
                }
                else
                {
                    Debug.LogError("KWS2 Water Unknown RenderPipeline: " + rpName);
                }
            }
        }
        
        static string Remove(string input, string keyword)
        {
            return input.Replace(keyword + ";", "").Replace(";" + keyword, "").Replace(keyword, "");
        }
    }
}
#endif