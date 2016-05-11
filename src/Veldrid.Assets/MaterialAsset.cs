﻿using System.Linq;
using Veldrid.Graphics;

namespace Veldrid.Assets
{
    public class MaterialAsset
    {
        public string Name { get; set; } = "NONAME";
        public string VertexShader { get; set; } = "NOVERTEXSHADER";
        public string FragmentShader { get; set; } = "NOFRAGMENTSHADER";
        public MaterialVertexInput VertexInputs { get; set; } = new MaterialVertexInput();
        public MaterialGlobalInputDescription[] GlobalInputs { get; set; } = new MaterialGlobalInputDescription[0];
        public MaterialPerObjectInputElement[] PerObjectInputs { get; set; } = new MaterialPerObjectInputElement[0];
        public ContextTextureInputElement[] ContextTextures { get; set; } = new ContextTextureInputElement[0];
        public TextureAsset[] TextureInputs { get; set; } = new TextureAsset[0];

        public Material Create(AssetDatabase ad, RenderContext rc)
        {
            MaterialTextureInputElement[] texElements = TextureInputs.Select(ta => ta.Create(ad)).ToArray();
            var allTextures = texElements.Concat(ContextTextures).ToArray();
            var materialTextureInputs = new MaterialTextureInputs(allTextures);
            MaterialInputs<MaterialGlobalInputElement> globalInputs = new MaterialInputs<MaterialGlobalInputElement>(GlobalInputs.Select(mgid => mgid.Create(rc)).ToArray());
            MaterialInputs<MaterialPerObjectInputElement> perObjectInputs = new MaterialInputs<MaterialPerObjectInputElement>(PerObjectInputs);
            return rc.ResourceFactory.CreateMaterial(rc, VertexShader, FragmentShader, VertexInputs, globalInputs, perObjectInputs, materialTextureInputs);
        }

        public struct NamedAssetTexture
        {
            public readonly string Name;
            public readonly TextureAsset TextureAsset;

            public NamedAssetTexture(string name, TextureAsset textureAsset)
            {
                Name = name;
                TextureAsset = textureAsset;
            }
        }
    }

    public class MaterialGlobalInputDescription
    {
        public string Name { get; set; }
        public MaterialInputType Type { get; set; }
        public string ProviderName { get; set; }

        public MaterialGlobalInputElement Create(RenderContext rc)
        {
            return new MaterialGlobalInputElement(Name, Type, rc.DataProviders[ProviderName]);
        }
    }
}
