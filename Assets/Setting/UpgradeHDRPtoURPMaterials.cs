using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using UnityEditor;
using System;

namespace LinceWorks
{
    class UpgradeHDRPtoURPMaterials
    {
        static List<MaterialUpgrader> GetHDtoURPUpgraders()
        {
            var upgraders = new List<MaterialUpgrader>();
            upgraders.Add(new HDLitToURPLitMaterialUpgrader("HDRP/Lit", "Universal Render Pipeline/Lit", HDLitToURPLitMaterialUpgrader.ConvertHDRPtoURPMaterialKeywords));
            return upgraders;
        }

        [MenuItem("Edit/Render Pipeline/Convert Project HDRP Materials to URP", priority = CoreUtils.editMenuPriority2)]
        internal static void UpgradeMaterialsProject()
        {
            MaterialUpgrader.UpgradeProjectFolder(GetHDtoURPUpgraders(), "Converting...");
        }

        [MenuItem("Edit/Render Pipeline/Convert Selected HDRP Materials to URP", priority = CoreUtils.editMenuPriority2)]
        internal static void UpgradeMaterialsSelection()
        {
            MaterialUpgrader.UpgradeSelection(GetHDtoURPUpgraders(), "Converting...");
        }
    }

    class HDLitToURPLitMaterialUpgrader : MaterialUpgrader
    {
        public HDLitToURPLitMaterialUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer = null)
        {
            if (sourceShaderName == null)
                throw new ArgumentNullException("oldShaderName");

            RenameShader(sourceShaderName, destShaderName, finalizer);

            RenameTexture("_BaseColorMap", "_BaseMap");
            //RenameColor("_Color", "_BaseColor");
            //RenameFloat("_Glossiness", "_Smoothness");
            RenameTexture("_NormalMap", "_BumpMap");
            RenameTexture("_MaskMap", "_MetallicGlossMap");
            RenameFloat("_NormalScale", "_BumpScale");
            //RenameTexture("_ParallaxMap", "_HeightMap");
            RenameTexture("_EmissiveColorMap", "_EmissionMap");
            RenameColor("_EmissiveColor", "_EmissionColor");
            //RenameTexture("_DetailAlbedoMap", "_DetailMap");
            //RenameFloat("_UVSec", "_UVDetail");
            //SetFloat("_LinkDetailsWithBase", 0);
            //RenameFloat("_DetailNormalMapScale", "_DetailNormalScale");
            RenameFloat("_AlphaCutoff", "_Cutoff");

            //RenameKeywordToFloat("_ALPHATEST_ON", "_AlphaCutoffEnable", 1f, 0f);

            //if (sourceShaderName == Lit)
            //{
            //    SetFloat("_MaterialID", 1f);
            //}

            //if (sourceShaderName == Standard_Spec)
            //{
            //    SetFloat("_MaterialID", 4f);

            //    RenameColor("_SpecColor", "_SpecularColor");
            //    RenameTexture("_SpecGlossMap", "_SpecularColorMap");
            //}


        }

        static void UpdateSurfaceTypeAndBlendMode(Material material)
        {
            // Property _Mode is already renamed to _Surface at this point
            if (material.HasProperty("_SurfaceType"))
                material.SetInt("_Surface", (int)material.GetFloat("_SurfaceType"));
            if (material.HasProperty("_BlendMode"))
                material.SetInt("_Blend", (int)material.GetFloat("_BlendMode"));
        }

        public static void ConvertHDRPtoURPMaterialKeywords(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            if (material.GetTexture("_MetallicGlossMap"))
                material.SetFloat("_Smoothness", 1);
            //else
            //    material.SetFloat("_Smoothness", material.GetFloat("_Glossiness"));

            material.SetFloat("_WorkflowMode", 1.0f);
            CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
            UpdateSurfaceTypeAndBlendMode(material);
            BaseShaderGUI.SetupMaterialBlendMode(material);
        }
    }


}
