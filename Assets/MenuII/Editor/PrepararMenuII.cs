using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class PrepararMenuII
{
    const string Root="Assets/MenuII";
    const string Pending="VerificacionMenuII/preparar.pendiente";
    static readonly Color Navy=Hex("0A3D4A");
    static readonly Color Cream=Hex("E8F4F4");
    static readonly Color Gold=Hex("F0C14B");
    static TMP_FontAsset font;
    static Sprite buttonSprite,roundSprite,trackSprite,fillSprite,handleSprite,fieldSprite;
    static readonly Dictionary<string,Sprite> icons=new Dictionary<string,Sprite>();

    static PrepararMenuII() => EditorApplication.update+=Revisar;
    static void Revisar()
    {
        if(!File.Exists(Pending) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update-=Revisar;
        Crear();
    }

    public static void Crear()
    {
        try
        {
            Directory.CreateDirectory("VerificacionMenuII");
            if(SceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            foreach(string folder in new[]{"Sprites","Prefabs","Audio"}) Directory.CreateDirectory(Root+"/"+folder);
            AssetDatabase.Refresh();
            ConfigurarCalidad();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(font==null) throw new Exception("Faltan los recursos de TextMeshPro.");
            Skin();
            fieldSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FreeButtonSet/Textures/controls/universal_panel_10.png");
            CrearEscena();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Menu.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Nivel1.unity",true)};
            PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground=true;
            AssetDatabase.SaveAssets();
            if(File.Exists(Pending)) File.Delete(Pending);
            File.WriteAllText("VerificacionMenuII/preparacion.txt","MENU_II_PREPARADO\nCuatro paneles, cinco botones, tres botones Volver.\nInput 16 caracteres; calidad 4 niveles; brillo 0.8; pantalla completa; volumen y musica del usuario.\n");
            VerificacionMenuIIEditor.Iniciar();
        }
        catch(Exception e)
        {
            if(File.Exists(Pending)) File.Delete(Pending);
            Debug.LogException(e); File.WriteAllText("VerificacionMenuII/error-preparacion.txt",e.ToString());
        }
    }

    static void ConfigurarCalidad()
    {
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
        var profiles=settings.FindProperty("m_QualitySettings");
        if(profiles.arraySize==6) {profiles.DeleteArrayElementAtIndex(4);profiles.DeleteArrayElementAtIndex(0);}
        string[] names={"Baja","Media","Alta","Ultra"};
        if(profiles.arraySize!=4) throw new Exception("Se esperaban cuatro niveles de calidad.");
        for(int i=0;i<4;i++) profiles.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue=names[i];
        settings.FindProperty("m_CurrentQuality").intValue=2;
        var defaults=settings.FindProperty("m_PerPlatformDefaultQuality");
        for(int i=0;i<defaults.arraySize;i++) defaults.GetArrayElementAtIndex(i).FindPropertyRelative("second").intValue=2;
        settings.ApplyModifiedPropertiesWithoutUndo();
        QualitySettings.SetQualityLevel(2,true);
    }

    static void CrearEscena()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)); camera.tag="MainCamera";
        camera.transform.position=new Vector3(0,0,-10); camera.GetComponent<Camera>().orthographic=true;
        camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor; camera.GetComponent<Camera>().backgroundColor=Navy;
        var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var canvas=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
        var controller=new GameObject("MenuController").AddComponent<MenuController>();
        var main=Panel("MainPanel",canvas.transform,Navy);
        var settings=Panel("SettingsPanel",canvas.transform,Navy);
        var scores=Panel("ScoresPanel",canvas.transform,Navy);
        var credits=Panel("CreditsPanel",canvas.transform,Navy);
        controller.mainPanel=main; controller.settingsPanel=settings; controller.scoresPanel=scores; controller.creditsPanel=credits;
        var layout=main.AddComponent<VerticalLayoutGroup>(); layout.spacing=24; layout.childAlignment=TextAnchor.MiddleCenter;
        layout.childControlWidth=false; layout.childControlHeight=false; layout.childForceExpandWidth=false; layout.childForceExpandHeight=false;
        var title=Label("Titulo","Inicio",main.transform,72,Cream); title.fontStyle=FontStyles.Bold;
        Ignorar(title.gameObject); Place(title.rectTransform,new Vector2(.5f,1),new Vector2(0,-115),new Vector2(1200,100));
        var prototype=CreateButton(main.transform,"Jugar","play");
        string prefabPath=Root+"/Prefabs/BotonMenu.prefab";
        PrefabUtility.SaveAsPrefabAsset(prototype.gameObject,prefabPath); Object.DestroyImmediate(prototype.gameObject);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        controller.menuButtons=new[]{MenuButton(prefab,main.transform,"Jugar","play"),MenuButton(prefab,main.transform,"Ajustes","gear"),MenuButton(prefab,main.transform,"Puntuaciones","trophy"),MenuButton(prefab,main.transform,"Créditos","person"),MenuButton(prefab,main.transform,"Salir","exit")};

        ConfigureSecondary(settings,"Ajustes");
        controller.playerNameField=Input(settings.transform);
        var qualityLabel=Label("Etiqueta calidad","Calidad gráfica",settings.transform,32,Gold); qualityLabel.alignment=TextAlignmentOptions.MidlineLeft; Preferred(qualityLabel.gameObject,560,40);
        controller.qualityDropdown=Dropdown(settings.transform);
        controller.brightnessValue=SettingLabel(settings.transform,"Brillo","80 %");
        controller.brightnessSlider=CreateSlider(settings.transform,"BrightnessSlider",.8f);
        controller.volumeValue=SettingLabel(settings.transform,"Volumen","100 %");
        controller.volumeSlider=CreateSlider(settings.transform,"VolumeSlider",1);
        controller.fullscreenToggle=CreateToggle(settings.transform);
        var settingsBack=Back(settings.transform);

        ConfigureSecondary(scores,"Mejores puntuaciones");
        scores.transform.Find("Titulo").GetComponent<TextMeshProUGUI>().fontSize=56;
        foreach(string line in new[]{"1. JugadorA — 12500","2. JugadorB — 9800","3. JugadorC — 7600","4. JugadorD — 5400"})
            Preferred(Label("Puntuacion de ejemplo",line,scores.transform,36,Cream).gameObject,1000,80);
        var scoresBack=Back(scores.transform);

        var creditText=Label("Texto de creditos","Proyecto Lindavista\nActividad de creación de menú II\n\nDesarrollado con Unity\n\nGracias por jugar.",credits.transform,42,Cream);
        creditText.lineSpacing=20;
        Place(creditText.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1400,650));
        var creditsBack=Back(credits.transform);
        controller.backButtons=new[]{settingsBack,scoresBack,creditsBack}; controller.gameSceneName="Nivel1";

        var music=new GameObject("MusicPlayer",typeof(AudioSource)).GetComponent<AudioSource>();
        music.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/prettyjohn1-halloween-halloween-music-620626.mp3");
        if(music.clip==null) throw new Exception("El audio del usuario no esta importado.");
        music.loop=true; music.playOnAwake=true; music.spatialBlend=0; music.volume=.35f;
        main.SetActive(true);settings.SetActive(false);scores.SetActive(false);credits.SetActive(false);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/Menu.unity");
    }

    static TMP_Text SettingLabel(Transform parent,string caption,string value)
    {
        var label=Label("Etiqueta "+caption,caption,parent,36,Gold); label.alignment=TextAlignmentOptions.MidlineLeft;
        Preferred(label.gameObject,560,50);
        var valueLabel=Label("Valor "+caption,value,label.transform,30,Cream);
        Stretch(valueLabel.rectTransform); valueLabel.alignment=TextAlignmentOptions.MidlineRight;
        return valueLabel;
    }

    static TMP_DefaultControls.Resources Resources()
    {
        return new TMP_DefaultControls.Resources{standard=fieldSprite,background=fieldSprite,inputField=fieldSprite,knob=handleSprite,checkmark=handleSprite,dropdown=icons["down"],mask=fieldSprite};
    }
    static TMP_InputField Input(Transform parent)
    {
        var go=TMP_DefaultControls.CreateInputField(Resources()); go.name="PlayerNameField"; go.transform.SetParent(parent,false);
        Preferred(go,560,55); go.GetComponent<Image>().color=Hex("06303B");
        var field=go.GetComponent<TMP_InputField>(); field.contentType=TMP_InputField.ContentType.Standard; field.characterLimit=16;
        var placeholder=(TMP_Text)field.placeholder; placeholder.text="Tu nombre…"; placeholder.font=font; placeholder.fontSize=32; placeholder.color=Hex("88AAAA");
        field.textComponent.font=font;field.textComponent.fontSize=36;field.textComponent.color=Cream;
        field.textComponent.textWrappingMode=TextWrappingModes.NoWrap; field.caretColor=Hex("3ECFCF");field.customCaretColor=true;
        return field;
    }
    static TMP_Dropdown Dropdown(Transform parent)
    {
        var go=TMP_DefaultControls.CreateDropdown(Resources()); go.name="QualityDropdown"; go.transform.SetParent(parent,false);
        Preferred(go,560,55); go.GetComponent<Image>().color=Hex("06303B");
        var dropdown=go.GetComponent<TMP_Dropdown>(); dropdown.ClearOptions(); dropdown.AddOptions(new List<string>{"Baja","Media","Alta","Ultra"}); dropdown.value=2;
        dropdown.captionText.font=font;dropdown.captionText.fontSize=34;dropdown.captionText.color=Cream;
        dropdown.itemText.font=font;dropdown.itemText.fontSize=30;dropdown.itemText.color=Cream;
        dropdown.template.sizeDelta=new Vector2(0,230);
        dropdown.template.GetComponent<Image>().color=Hex("125565");
        var item=dropdown.template.Find("Viewport/Content/Item").GetComponent<RectTransform>(); item.sizeDelta=new Vector2(0,50);
        dropdown.template.Find("Viewport/Content").GetComponent<RectTransform>().sizeDelta=new Vector2(0,60);
        dropdown.template.Find("Viewport/Content/Item/Item Background").GetComponent<Image>().color=Hex("1B6574");
        dropdown.template.Find("Viewport/Content/Item/Item Checkmark").GetComponent<Image>().color=Hex("3ECFCF");
        var toggle=item.GetComponent<Toggle>(); var colors=toggle.colors; colors.highlightedColor=Hex("3ECFCF"); colors.selectedColor=Hex("3ECFCF");toggle.colors=colors;
        return dropdown;
    }

    static GameObject Panel(string name,Transform parent,Color color)
    {
        var go=UI(name,parent,typeof(Image)); Stretch(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color=color; return go;
    }
    static GameObject UI(string name,Transform parent,params Type[] components)
    {
        var go=new GameObject(name,new[]{typeof(RectTransform)}.Concat(components).ToArray());
        go.transform.SetParent(parent,false); return go;
    }
    static TextMeshProUGUI Label(string name,string text,Transform parent,float size,Color color)
    {
        var go=UI(name,parent,typeof(TextMeshProUGUI));
        var label=go.GetComponent<TextMeshProUGUI>();
        label.font=font; label.text=text; label.fontSize=size; label.color=color;
        label.alignment=TextAlignmentOptions.Center; label.textWrappingMode=TextWrappingModes.NoWrap; label.raycastTarget=false;
        return label;
    }
    static void ConfigureSecondary(GameObject panel,string title)
    {
        var layout=panel.AddComponent<VerticalLayoutGroup>();
        layout.padding=new RectOffset(100,100,70,70); layout.spacing=32; layout.childAlignment=TextAnchor.UpperCenter;
        layout.childControlWidth=true; layout.childControlHeight=true;
        layout.childForceExpandWidth=false; layout.childForceExpandHeight=false;
        Preferred(Label("Titulo",title,panel.transform,60,Cream).gameObject,1200,80);
    }
    static Button CreateButton(Transform parent,string text,string icon)
    {
        var go=UI(text,parent,typeof(Image),typeof(Button),typeof(HorizontalLayoutGroup));
        var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(580,95);
        var image=go.GetComponent<Image>(); image.sprite=buttonSprite; image.type=Image.Type.Sliced;
        var button=go.GetComponent<Button>(); button.targetGraphic=image; Colors(button);
        var layout=go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing=14; layout.childAlignment=TextAnchor.MiddleCenter;
        layout.childControlWidth=true; layout.childControlHeight=true;
        layout.childForceExpandWidth=false; layout.childForceExpandHeight=false;
        var symbol=UI("Icon",go.transform,typeof(Image));
        symbol.GetComponent<Image>().sprite=icons[icon]; symbol.GetComponent<Image>().raycastTarget=false;
        Preferred(symbol,44,44);
        var label=Label("Texto",text,go.transform,56,Navy); label.fontStyle=FontStyles.Bold;
        var element=label.gameObject.AddComponent<LayoutElement>(); element.preferredHeight=78;
        return button;
    }
    static Button MenuButton(GameObject prefab,Transform parent,string text,string icon)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
        go.name=text; go.GetComponentInChildren<TextMeshProUGUI>().text=text;
        go.transform.Find("Icon").GetComponent<Image>().sprite=icons[icon];
        return go.GetComponent<Button>();
    }
    static Button Back(Transform parent)
    {
        var go=UI("Volver",parent,typeof(Image),typeof(Button)); Ignorar(go);
        Place(go.GetComponent<RectTransform>(),new Vector2(0,1),new Vector2(65,-65),new Vector2(85,85));
        go.GetComponent<Image>().sprite=roundSprite;
        var button=go.GetComponent<Button>(); button.targetGraphic=go.GetComponent<Image>(); Colors(button,true);
        var symbol=UI("Icon",go.transform,typeof(Image));
        Place(symbol.GetComponent<RectTransform>(),new Vector2(.5f,.5f),Vector2.zero,new Vector2(44,44));
        symbol.GetComponent<Image>().sprite=icons["back"]; symbol.GetComponent<Image>().color=Navy; symbol.GetComponent<Image>().raycastTarget=false;
        return button;
    }
    static Slider CreateSlider(Transform parent,string name,float initial)
    {
        var go=UI(name,parent,typeof(Slider)); Preferred(go,560,48);
        var background=UI("Background",go.transform,typeof(Image));
        Horizontal(background.GetComponent<RectTransform>(),16,0,0);
        background.GetComponent<Image>().sprite=trackSprite; background.GetComponent<Image>().type=Image.Type.Sliced;
        var fillArea=UI("Fill Area",go.transform); Horizontal(fillArea.GetComponent<RectTransform>(),16,0,20);
        var fill=UI("Fill",fillArea.transform,typeof(Image)); Stretch(fill.GetComponent<RectTransform>());
        fill.GetComponent<Image>().sprite=fillSprite; fill.GetComponent<Image>().type=Image.Type.Sliced;
        fill.GetComponent<Image>().color=Hex("3ECFCF");
        var handleArea=UI("Handle Slide Area",go.transform); Stretch(handleArea.GetComponent<RectTransform>());
        handleArea.GetComponent<RectTransform>().offsetMin=new Vector2(20,0); handleArea.GetComponent<RectTransform>().offsetMax=new Vector2(-20,0);
        var handle=UI("Handle",handleArea.transform,typeof(Image));
        handle.GetComponent<RectTransform>().anchorMin=new Vector2(0,0); handle.GetComponent<RectTransform>().anchorMax=new Vector2(0,1);
        handle.GetComponent<RectTransform>().sizeDelta=new Vector2(40,0);
        handle.GetComponent<Image>().sprite=handleSprite; handle.GetComponent<Image>().preserveAspect=true;
        var slider=go.GetComponent<Slider>(); slider.fillRect=fill.GetComponent<RectTransform>(); slider.handleRect=handle.GetComponent<RectTransform>();
        slider.targetGraphic=handle.GetComponent<Image>(); slider.minValue=0; slider.maxValue=1; slider.wholeNumbers=false; slider.value=initial;
        return slider;
    }
    static Toggle CreateToggle(Transform parent)
    {
        var go=UI("FullscreenToggle",parent,typeof(Toggle)); Preferred(go,560,65);
        var bg=UI("Background",go.transform,typeof(Image));
        Place(bg.GetComponent<RectTransform>(),new Vector2(1,.5f),Vector2.zero,new Vector2(60,60)); bg.GetComponent<RectTransform>().pivot=new Vector2(1,.5f);
        bg.GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FreeButtonSet/Textures/controls/toggle_bg_50.png"); bg.GetComponent<Image>().type=Image.Type.Sliced;
        var check=UI("Checkmark",bg.transform,typeof(Image));
        Place(check.GetComponent<RectTransform>(),new Vector2(.5f,.5f),Vector2.zero,new Vector2(34,34));
        check.GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FreeButtonSet/Textures/icons/64x64/checkmark.png"); check.GetComponent<Image>().color=Navy;
        var label=Label("Label","Pantalla completa",go.transform,36,Gold); label.alignment=TextAlignmentOptions.MidlineLeft;
        Stretch(label.rectTransform); label.rectTransform.offsetMax=new Vector2(-80,0);
        var toggle=go.GetComponent<Toggle>(); toggle.targetGraphic=bg.GetComponent<Image>(); toggle.graphic=check.GetComponent<Image>(); toggle.isOn=true;
        return toggle;
    }
    static void Preferred(GameObject go,float width,float height)
    {
        var le=go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>(); le.preferredWidth=width; le.preferredHeight=height;
    }
    static void Ignorar(GameObject go) => go.AddComponent<LayoutElement>().ignoreLayout=true;
    static void Stretch(RectTransform rt) {rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=Vector2.zero; rt.offsetMax=Vector2.zero;}
    static void Place(RectTransform rt,Vector2 anchor,Vector2 position,Vector2 size) {rt.anchorMin=rt.anchorMax=anchor; rt.anchoredPosition=position; rt.sizeDelta=size;}
    static void Horizontal(RectTransform rt,float height,float left,float right)
    {
        rt.anchorMin=new Vector2(0,.5f); rt.anchorMax=new Vector2(1,.5f); rt.sizeDelta=new Vector2(-left-right,height); rt.anchoredPosition=new Vector2((left-right)/2,0);
    }
    static void Colors(Selectable item,bool back=false)
    {
        var c=item.colors; c.normalColor=Color.white; c.highlightedColor=Hex("3ECFCF"); c.selectedColor=Hex("3ECFCF"); c.pressedColor=Hex("A0A0A0"); c.fadeDuration=.1f; item.colors=c;
    }
    static Color Hex(string hex) {ColorUtility.TryParseHtmlString("#"+hex,out var color); return color;}

    static void Skin()
    {
        const string pack="Assets/FreeButtonSet/Textures/";
        buttonSprite=AssetDatabase.LoadAssetAtPath<Sprite>(pack+"buttons/button_150.png");
        roundSprite=AssetDatabase.LoadAssetAtPath<Sprite>(pack+"buttons/button_round_130.png");
        trackSprite=AssetDatabase.LoadAssetAtPath<Sprite>(pack+"controls/universal_panel_20.png");
        fillSprite=AssetDatabase.LoadAssetAtPath<Sprite>(pack+"controls/radio_60.png");
        handleSprite=AssetDatabase.LoadAssetAtPath<Sprite>(pack+"controls/slider_handle.png");
        string[] names={"play","gear","person","exit","back","trophy","star","down"};
        string[] files={"play","settings","user","x","arrow_left","star","star","arrow_down"};
        for(int i=0;i<names.Length;i++)icons[names[i]]=AssetDatabase.LoadAssetAtPath<Sprite>(pack+"icons/64x64/"+files[i]+".png");
        if(buttonSprite==null || roundSprite==null)throw new Exception("Faltan los recursos de FreeButtonSet.");
    }
    static float Capsule(float x,float y,int width,int height)
    {
        float radius=height/2f; float cx=Mathf.Clamp(x+.5f,radius,width-radius); return Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(cx,radius))-radius;
    }
    static bool Icon(string name,float x,float y)
    {
        float r=Mathf.Sqrt(x*x+y*y);
        if(name=="down") return y<.3f && y>-.4f && Mathf.Abs(x)<(y+.4f)*.9f;
        if(name=="trophy") return (y>-.05f && y<.65f && Mathf.Abs(x)<.25f+(y+.05f)*.35f) || (Mathf.Abs(x)<.09f && y>-.48f && y<.05f) || (Mathf.Abs(x)<.4f && y>-.7f && y<-.48f) || (Mathf.Abs(x)>.43f && Mathf.Abs(x)<.68f && y>.08f && y<.5f);
        if(name=="star") return Star(x,y);
        if(name=="play") return x>-.52f && x<.68f && Mathf.Abs(y)<(.68f-x)*.63f;
        if(name=="gear") return r>.27f && r<.6f+.2f*(Mathf.Cos(Mathf.Atan2(y,x)*8)>0 ? 1 : 0);
        if(name=="person") return x*x+(y-.35f)*(y-.35f)<.25f*.25f || x*x/(.56f*.56f)+(y+.42f)*(y+.42f)/(.34f*.34f)<1;
        if(name=="back") x=-x;
        return (x>-.65f && x<.65f && Mathf.Abs(y)<.12f) || (x>.02f && x<.7f && Mathf.Abs(Mathf.Abs(y)-(.7f-x))<.15f);
    }
    static bool Star(float x,float y)
    {
        bool inside=false; var p=new Vector2(x,y);
        for(int i=0,j=9;i<10;j=i++)
        {
            float ai=i*Mathf.PI/5+Mathf.PI/2, aj=j*Mathf.PI/5+Mathf.PI/2;
            var a=new Vector2(Mathf.Cos(ai),Mathf.Sin(ai))*(i%2==0?.8f:.34f);
            var b=new Vector2(Mathf.Cos(aj),Mathf.Sin(aj))*(j%2==0?.8f:.34f);
            if((a.y>p.y)!=(b.y>p.y) && p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
        }
        return inside;
    }
    static Sprite Texture(string name,int width,int height,int border,Func<int,int,int,int,Color> pixel)
    {
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
        for(int y=0;y<height;y++) for(int x=0;x<width;x++) texture.SetPixel(x,y,pixel(x,y,width,height));
        texture.Apply(); string path=Root+"/Sprites/"+name+".png"; File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single; var spriteSettings=new TextureImporterSettings(); importer.ReadTextureSettings(spriteSettings); spriteSettings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(spriteSettings); importer.spritePixelsPerUnit=100; importer.alphaIsTransparency=true;
        importer.spriteBorder=Vector4.one*border; importer.mipmapEnabled=false; importer.filterMode=FilterMode.Bilinear;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
