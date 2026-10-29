using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public class MenuIIVerification : MonoBehaviour
{
    readonly List<string> checks=new List<string>();
    readonly List<string> errors=new List<string>();
    Mouse mouse;
    Keyboard keyboard;
    bool quitExpected;
    bool restored;
    static Dictionary<string,string> saved;
    public static readonly string[] Keys={MenuController.NameKey,MenuController.QualityKey,MenuController.BrightnessKey,MenuController.FullscreenKey,MenuController.VolumeKey};
    public static string Output
    {
        get
        {
            string arg=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--pruebas-salida="));
            return arg==null?Path.GetFullPath("VerificacionMenuII"):arg.Substring("--pruebas-salida=".Length);
        }
    }
    public static string Report => Path.Combine(Output,Application.isEditor?"resultado-editor.txt":"resultado-build.txt");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void BootPlayer()
    {
        if(Application.isEditor || !Environment.GetCommandLineArgs().Contains("--verificar-menu-ii")) return;
        SavePrefs(); ResetPrefs();
        SceneManager.sceneLoaded+=StartPlayer;
    }
    static void StartPlayer(Scene scene,LoadSceneMode mode)
    {
        SceneManager.sceneLoaded-=StartPlayer;
        new GameObject("Prueba temporal Menu II").AddComponent<MenuIIVerification>();
    }
    public static void SavePrefs()
    {
        saved=new Dictionary<string,string>();
        foreach(string key in Keys)
        {
            if(!PlayerPrefs.HasKey(key)) {saved[key]=null;continue;}
            if(key==MenuController.NameKey) saved[key]=PlayerPrefs.GetString(key);
            else if(key==MenuController.BrightnessKey || key==MenuController.VolumeKey) saved[key]=PlayerPrefs.GetFloat(key).ToString(System.Globalization.CultureInfo.InvariantCulture);
            else saved[key]=PlayerPrefs.GetInt(key).ToString();
        }
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output,Application.isEditor?"prefs-editor.txt":"prefs-build.txt"),saved.Select(p=>p.Key+"\t"+(p.Value??"<ausente>")));
    }
    public static void ResetPrefs()
    {
        foreach(string key in Keys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
    public static void RestorePrefs()
    {
        if(saved==null)
        {
            string path=Path.Combine(Output,Application.isEditor?"prefs-editor.txt":"prefs-build.txt");
            if(!File.Exists(path)) return;
            saved=File.ReadAllLines(path).Select(l=>l.Split(new[]{'\t'},2)).ToDictionary(a=>a[0],a=>a[1]=="<ausente>"?null:a[1]);
        }
        foreach(var pair in saved)
        {
            if(pair.Value==null) PlayerPrefs.DeleteKey(pair.Key);
            else if(pair.Key==MenuController.NameKey) PlayerPrefs.SetString(pair.Key,pair.Value);
            else if(pair.Key==MenuController.BrightnessKey || pair.Key==MenuController.VolumeKey) PlayerPrefs.SetFloat(pair.Key,float.Parse(pair.Value,System.Globalization.CultureInfo.InvariantCulture));
            else PlayerPrefs.SetInt(pair.Key,int.Parse(pair.Value));
        }
        PlayerPrefs.Save();
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived+=Log;
        SceneManager.sceneLoaded+=Mute;
        Mute(SceneManager.GetActiveScene(),LoadSceneMode.Single);
        StartCoroutine(Safe(Test()));
    }
    void Mute(Scene scene,LoadSceneMode mode)
    {
        foreach(var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)) source.mute=true;
    }
    void Log(string m,string stack,LogType type)
    {
        if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(m+"\n"+stack);
    }
    void Check(bool ok,string message)
    {
        if(!ok) throw new Exception(message);
        checks.Add("OK: "+message);
    }
    IEnumerator Safe(IEnumerator test)
    {
        var stack=new Stack<IEnumerator>(); stack.Push(test);
        while(stack.Count>0)
        {
            bool next=false;object current=null;Exception failure=null;
            try {next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;} catch(Exception e){failure=e;}
            if(failure!=null)
            {
                errors.Add(failure.ToString()); Write(false); RestorePrefs();restored=true;
#if UNITY_EDITOR
                UnityEditor.SessionState.SetString("MenuII.Prueba","fallo"); UnityEditor.EditorApplication.ExitPlaymode();
#else
                Application.Quit(1);
#endif
                yield break;
            }
            if(!next) {stack.Pop();continue;}
            if(current is IEnumerator nested) stack.Push(nested);
            else yield return current;
        }
        if(!quitExpected) {Write(false);Application.Quit(1);}
    }
    IEnumerator Click(RectTransform rt)
    {
        Canvas.ForceUpdateCanvases();
        Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return new WaitForSecondsRealtime(.12f);
    }
    IEnumerator Click(Button button) => Click((RectTransform)button.transform);
    static MenuController Menu() => Object.FindFirstObjectByType<MenuController>();
    void BindDevices()
    {
        var ui=Object.FindFirstObjectByType<InputSystemUIInputModule>();
        if(ui!=null) ui.actionsAsset.devices=new InputDevice[]{mouse,keyboard};
    }
    IEnumerator Test()
    {
        yield return new WaitForSecondsRealtime(.3f);
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        mouse=InputSystem.AddDevice<Mouse>("MenuII.VirtualMouse");keyboard=InputSystem.AddDevice<Keyboard>("MenuII.VirtualKeyboard");BindDevices();
        var menu=Menu();
        Check(menu!=null && SceneManager.GetActiveScene().name=="Menu","Inicio en Menu con MenuController");
        Check(menu.mainPanel.activeSelf && !menu.settingsPanel.activeSelf && !menu.scoresPanel.activeSelf && !menu.creditsPanel.activeSelf,"Cuatro paneles y solo Inicio visible");
        var canvas=GameObject.Find("Canvas").GetComponent<CanvasScaler>();
        Check(canvas.referenceResolution==new Vector2(1920,1080) && canvas.matchWidthOrHeight==.5f,"Canvas 1920x1080 con Match 0.5");
        var layout=menu.mainPanel.GetComponent<VerticalLayoutGroup>();
        Check(layout.spacing==24 && !layout.childControlHeight && !layout.childControlWidth && !layout.childForceExpandHeight && !layout.childForceExpandWidth,"MainPanel: spacing 24, centrado y sin estirar hijos");
        Check(menu.menuButtons.Length==5 && menu.backButtons.Length==3,"Cinco botones principales y tres botones Volver");
        foreach(var button in menu.menuButtons)
        {
            Check(((RectTransform)button.transform).sizeDelta==new Vector2(580,95),button.name+": tamano 580x95");
            Check(button.GetComponent<Image>().type==Image.Type.Sliced && button.GetComponent<Image>().sprite.border.x>0,button.name+": sprite con nueve partes");
            Check(button.GetComponent<HorizontalLayoutGroup>().spacing==14 && button.transform.GetChild(0).name=="Icon",button.name+": icono antes del texto y spacing 14");
        }
        var music=GameObject.Find("MusicPlayer").GetComponent<AudioSource>();
        Check(music.clip!=null && music.loop && music.playOnAwake && music.isPlaying,"MusicPlayer reproduce en bucle el audio de Halloween proporcionado");
        Capture("01-Inicio.png");
        yield return Click(menu.menuButtons[1]);
        Check(menu.settingsPanel.activeSelf && !menu.mainPanel.activeSelf,"Ajustes se abre con un clic real");
        var settingsLayout=menu.settingsPanel.GetComponent<VerticalLayoutGroup>();
        Check(settingsLayout.padding.left==100 && settingsLayout.padding.top==70 && settingsLayout.spacing==32,"SettingsPanel: padding 100/70 y spacing 32");
        Check(menu.playerNameField.characterLimit==16 && menu.playerNameField.contentType==TMP_InputField.ContentType.Standard,"Nombre: Standard y limite de 16 caracteres");
        Check(menu.qualityDropdown.options.Select(o=>o.text).SequenceEqual(new[]{"Baja","Media","Alta","Ultra"}) && menu.qualityDropdown.value==2,"Dropdown Baja/Media/Alta/Ultra, valor inicial Alta");
        Check(QualitySettings.names.SequenceEqual(new[]{"Baja","Media","Alta","Ultra"}) && QualitySettings.GetQualityLevel()==2,"Cuatro perfiles reales de calidad y Alta activa");
        Check(Mathf.Approximately(menu.brightnessSlider.value,.8f) && menu.fullscreenToggle.isOn,"Brillo inicial 0.8 y pantalla completa activada");
        Check(menu.volumeSlider!=menu.brightnessSlider,"Volumen y brillo usan controles independientes");
        Capture("02-Ajustes.png");
        yield return Click((RectTransform)menu.playerNameField.transform);
        menu.playerNameField.ActivateInputField();yield return null;
        string name="JugadorPrueba123456789";
        foreach(char c in name) menu.playerNameField.ProcessEvent(new Event{type=EventType.KeyDown,character=c,keyCode=KeyCode.None});
        Check(menu.playerNameField.text==name.Substring(0,16),"El campo acepta escritura y corta al caracter 16");
        yield return Click((RectTransform)menu.qualityDropdown.transform);
        var list=GameObject.Find("Dropdown List");
        Check(list!=null,"Dropdown despliega sus opciones con un clic real");
        Capture("03-Calidades.png");
        var low=list.GetComponentsInChildren<Toggle>().First(t=>t.GetComponentInChildren<TMP_Text>().text=="Baja");
        yield return Click((RectTransform)low.transform);
        Check(menu.qualityDropdown.value==0 && QualitySettings.GetQualityLevel()==0,"Elegir Baja cambia QualitySettings");
        for(int i=1;i<4;i++){menu.qualityDropdown.value=i;Check(QualitySettings.GetQualityLevel()==i,"Calidad real aplicada: "+menu.qualityDropdown.options[i].text);}
        menu.qualityDropdown.value=2;
        float volumeBefore=AudioListener.volume;menu.brightnessSlider.value=.2f;
        var brightness=Object.FindFirstObjectByType<BrilloGlobal>();
        Check(brightness!=null && Mathf.Approximately(brightness.Alpha,.6f) && AudioListener.volume==volumeBefore,"Brillo oscurece realmente la imagen sin modificar el volumen");
        Capture("04-Brillo-bajo.png");menu.brightnessSlider.value=.8f;
        menu.volumeSlider.value=.4f;
        Check(Mathf.Approximately(AudioListener.volume,.4f) && Mathf.Approximately(brightness.Value,.8f),"Volumen modifica el audio sin cambiar el brillo");
        var toggleRect=(RectTransform)menu.fullscreenToggle.transform;
        Vector2 p=RectTransformUtility.WorldToScreenPoint(null,toggleRect.TransformPoint(new Vector2(toggleRect.rect.xMax-30,toggleRect.rect.center.y)));
        InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=p,buttons=1});yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return new WaitForSecondsRealtime(.5f);
        Check(!menu.fullscreenToggle.isOn && PlayerPrefs.GetInt(MenuController.FullscreenKey)==0,"Toggle guarda el modo ventana");
        if(!Application.isEditor) Check(!Screen.fullScreen,"BUILD: el toggle cambia a modo ventana");
        menu.fullscreenToggle.isOn=true;yield return new WaitForSecondsRealtime(.7f);
        if(!Application.isEditor) Check(Screen.fullScreen,"BUILD: el toggle activa pantalla completa");
        menu.fullscreenToggle.isOn=false;yield return new WaitForSecondsRealtime(.5f);
        if(!Application.isEditor) Check(!Screen.fullScreen,"BUILD: puede volver a modo ventana");
        yield return Click(menu.backButtons[0]);
        Check(menu.mainPanel.activeSelf && PlayerPrefs.GetString(MenuController.NameKey)==name.Substring(0,16),"Volver desde Ajustes conserva el nombre");
        yield return Click(menu.menuButtons[2]);
        Check(menu.scoresPanel.activeSelf && menu.scoresPanel.GetComponentsInChildren<TMP_Text>().Count(t=>t.text.Contains("Jugador"))==4,"Puntuaciones muestra cuatro registros de ejemplo");
        Capture("05-Puntuaciones.png");yield return Click(menu.backButtons[1]);Check(menu.mainPanel.activeSelf,"Volver desde Puntuaciones funciona");
        yield return Click(menu.menuButtons[3]);Check(menu.creditsPanel.activeSelf,"Creditos abre su panel");
        Capture("06-Creditos.png");yield return Click(menu.backButtons[2]);Check(menu.mainPanel.activeSelf,"Volver desde Creditos funciona");
        yield return Click(menu.menuButtons[0]);yield return new WaitForSecondsRealtime(.2f);
        Check(SceneManager.GetActiveScene().name=="Nivel1","Jugar carga Nivel1");
        Check(Object.FindFirstObjectByType<BrilloGlobal>()!=null && Mathf.Approximately(Object.FindFirstObjectByType<BrilloGlobal>().Value,.8f),"El brillo se conserva en el juego");
        Check(GameObject.FindGameObjectsWithTag("Moneda").Length+GameObject.FindGameObjectsWithTag("Enemigo").Length==8,"Las ocho frutas originales siguen presentes");
        Camera.main.aspect=1280f/720f;yield return null;Capture("07-Nivel1.png");
        var body=GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        foreach(string tag in new[]{"Moneda","Enemigo"})
        {
            var fruit=GameObject.FindGameObjectsWithTag(tag).First();string label=fruit.name;
            body.position=fruit.transform.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();
            yield return new WaitForSecondsRealtime(.15f);Check(fruit==null,"Recogida original conservada: "+label);
        }
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return new WaitForSecondsRealtime(.2f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;BindDevices();menu=Menu();
        Check(menu!=null && SceneManager.GetActiveScene().name=="Menu","Escape regresa al menu sin modificar Nivel1");
        Check(menu.playerNameField.text==name.Substring(0,16) && menu.qualityDropdown.value==2 && Mathf.Approximately(menu.brightnessSlider.value,.8f),"Los ajustes se conservan al regresar del juego");
        Check(errors.Count==0,"Navegacion, musica y recogida sin errores");
        Write(true);quitExpected=true;
#if UNITY_EDITOR
        UnityEditor.SessionState.SetString("MenuII.Prueba","salir");
#endif
        yield return Click(menu.menuButtons[4]);
        throw new Exception("Salir no cerro el juego.");
    }
    void Write(bool success)
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Report,(success?"APROBADO\n":"FALLO\n")+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    public static void Capture(string name)
    {
        string folder=Path.Combine(Output,Application.isEditor?"Capturas":"CapturasBuild");Directory.CreateDirectory(folder);
        var camera=Camera.main;var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();
        var target=new RenderTexture(1280,720,24);camera.targetTexture=target;camera.aspect=1280f/720f;
        foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=canvas.sortingOrder>30000?.5f:1;}
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
        File.WriteAllBytes(Path.Combine(folder,name),image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;
        for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}
        Canvas.ForceUpdateCanvases();Object.Destroy(image);Object.Destroy(target);
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded-=Mute;Application.logMessageReceived-=Log;
        if(!restored) RestorePrefs();
    }
    void OnApplicationQuit() {RestorePrefs();restored=true;}
}
