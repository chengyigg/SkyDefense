using UnityEngine;
using UnityEditor;
using System.Text;

public class MaterialPropertyInspector : EditorWindow
{
    private Material _mat;
    private Vector2 _scroll;
    private string _output = "";

    [MenuItem("Tools/材质属性查看器")]
    public static void Open()
    {
        GetWindow<MaterialPropertyInspector>("材质属性查看器");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("把材质拖到这里", MessageType.Info);

        _mat = (Material)EditorGUILayout.ObjectField("材质", _mat, typeof(Material), false);

        if (GUILayout.Button("分析") && _mat != null)
            Analyze();

        if (GUILayout.Button("复制"))
            EditorGUIUtility.systemCopyBuffer = _output;

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        _output = EditorGUILayout.TextArea(_output, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    void Analyze()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"材质: {_mat.name}");
        sb.AppendLine($"Shader: {_mat.shader.name}");
        sb.AppendLine();

        Shader s = _mat.shader;
        int count = s.GetPropertyCount();
        sb.AppendLine($"========= 材质属性（共 {count} 个） =========");

        for (int i = 0; i < count; i++)
        {
            string name = s.GetPropertyName(i);
            var type = s.GetPropertyType(i);
            string valueStr = "";

            try
            {
                switch (type)
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        valueStr = _mat.GetColor(name).ToString();
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        valueStr = _mat.GetFloat(name).ToString();
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        valueStr = _mat.GetVector(name).ToString();
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        var tex = _mat.GetTexture(name);
                        valueStr = tex == null ? "null" : tex.name;
                        break;
                }
            }
            catch { }

            sb.AppendLine($"  [{type}] {name} = {valueStr}");
        }

        _output = sb.ToString();
        Debug.Log("分析完成");
    }
}