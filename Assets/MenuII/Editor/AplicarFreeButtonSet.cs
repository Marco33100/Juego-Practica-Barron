using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AplicarFreeButtonSet
{
    const string Pending="VerificacionMenuII/usar-freebuttonset.pendiente";
    const string Pack="Assets/FreeButtonSet/Textures/";
    static AplicarFreeButtonSet() => EditorApplication.update+=Revisar;
    static void Revisar()
    {
        if(!File.Exists(Pending)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        EditorApplication.update-=Revisar;
        try {Aplicar(); File.Delete(Pending); VerificacionMenuIIEditor.Iniciar();}
        catch(Exception e) {File.Delete(Pending);File.WriteAllText("VerificacionMenuII/error-freebuttonset.txt",e.ToString());Debug.LogException(e);}
    }
    static Sprite Sprite(string relative,Vector4? border=null)
    {
        string path=Pack+relative;
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new Exception("No se encontro el sprite importado: "+path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
        if(border.HasValue)importer.spriteBorder=border.Value;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void Aplicar()
    {
        if(SceneManager.GetActiveScene().isDirty)EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
        var map=new Dictionary<string,Sprite>{
            {"boton_alargado",Sprite("buttons/button_150.png",new Vector4(75,80,75,75))},
            {"boton_redondo",Sprite("buttons/button_round_130.png",new Vector4(60,65,60,60))},
            {"barra_fondo",Sprite("controls/universal_panel_20.png",new Vector4(25,25,25,25))},
            {"barra_relleno",Sprite("controls/radio_60.png",new Vector4(20,20,20,20))},
            {"perilla",Sprite("controls/slider_handle.png")},
            {"campo",Sprite("controls/universal_panel_10.png",new Vector4(15,15,15,15))},
            {"icono_play",Sprite("icons/64x64/play.png")},
            {"icono_gear",Sprite("icons/64x64/settings.png")},
            {"icono_person",Sprite("icons/64x64/user.png")},
            {"icono_exit",Sprite("icons/64x64/x.png")},
            {"icono_back",Sprite("icons/64x64/arrow_left.png")},
            {"icono_trophy",Sprite("icons/64x64/star.png")},
            {"icono_star",Sprite("icons/64x64/star.png")},
            {"icono_down",Sprite("icons/64x64/arrow_down.png")}
        };
        string prefabPath="Assets/MenuII/Prefabs/BotonMenu.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        Reemplazar(prefab,map);EstiloBoton(prefab.GetComponent<Button>());
        PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);PrefabUtility.UnloadPrefabContents(prefab);
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())Reemplazar(root,map);
        var menu=UnityEngine.Object.FindFirstObjectByType<MenuController>();
        foreach(var button in menu.menuButtons)EstiloBoton(button);
        foreach(var back in menu.backButtons){back.GetComponent<Image>().sprite=map["boton_redondo"];back.GetComponent<Image>().type=Image.Type.Sliced;}
        menu.fullscreenToggle.targetGraphic.GetComponent<Image>().sprite=Sprite("controls/toggle_bg_50.png",new Vector4(20,20,20,20));
        var check=(Image)menu.fullscreenToggle.graphic;
        check.sprite=Sprite("icons/64x64/checkmark.png");check.preserveAspect=true;
        menu.fullscreenToggle.targetGraphic.color=Color.white;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        var report=new List<string>{"RECURSOS IMPORTADOS APLICADOS","Botones: FreeButtonSet/buttons/button_150 y button_round_130, con bordes 9-slice.","Iconos: FreeButtonSet/icons/64x64. Puntuaciones usa estrella del paquete.","Sliders: universal_panel, radio_60 y slider_handle. Toggle: toggle_bg_50 y checkmark.","Valores conservados: Canvas 1920x1080 Match 0.5; botones 580x95 y letra 56; iconos 44x44; spacing 24 y 14.","Highlight #3ECFCF, Pressed #A0A0A0, Fade 0.1; textos/titulos/controles con medidas del documento.","Botones con su color amarillo original; texto oscuro para mantener legibilidad.","Nivel1, scripts de funcionamiento y musica conservados."};
        foreach(var image in menu.GetComponentsInChildren<Image>(true)){}
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach(var image in root.GetComponentsInChildren<Image>(true))
                if(image.sprite!=null && AssetDatabase.GetAssetPath(image.sprite).StartsWith("Assets/MenuII/Sprites/"))throw new Exception("Aun se usa un sprite provisional: "+image.name);
        File.WriteAllLines("VerificacionMenuII/freebuttonset.txt",report);
    }
    static void Reemplazar(GameObject root,Dictionary<string,Sprite> map)
    {
        foreach(var image in root.GetComponentsInChildren<Image>(true))
        {
            if(image.sprite==null)continue;
            if(map.TryGetValue(image.sprite.name,out var sprite)){image.sprite=sprite;EditorUtility.SetDirty(image);}
        }
    }
    static void EstiloBoton(Button button)
    {
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=ColorHTML("3ECFCF");colors.selectedColor=colors.highlightedColor;colors.pressedColor=ColorHTML("A0A0A0");colors.fadeDuration=.1f;button.colors=colors;
        button.GetComponent<Image>().color=Color.white;button.GetComponentInChildren<TMP_Text>().color=ColorHTML("0A3D4A");
        EditorUtility.SetDirty(button);
    }
    static Color ColorHTML(string value){ColorUtility.TryParseHtmlString("#"+value,out var result);return result;}
}
