using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerificacionMenuIIEditor
{
    static System.Diagnostics.Process player;
    static double playerStarted;
    static VerificacionMenuIIEditor() => EditorApplication.playModeStateChanged+=Cambio;

    public static void Iniciar()
    {
        MenuIIVerification.SavePrefs();MenuIIVerification.ResetPrefs();
        SessionState.SetString("MenuII.Prueba","entrar");
        EditorApplication.delayCall+=()=>EditorApplication.delayCall+=EditorApplication.EnterPlaymode;
    }
    static void Cambio(PlayModeStateChange state)
    {
        string phase=SessionState.GetString("MenuII.Prueba","");
        if(state==PlayModeStateChange.EnteredPlayMode && phase=="entrar")
        {
            SessionState.SetString("MenuII.Prueba","probando");
            new GameObject("Verificacion temporal Menu II").AddComponent<MenuIIVerification>();
        }
        if(state==PlayModeStateChange.EnteredEditMode && (phase=="salir" || phase=="fallo"))
            EditorApplication.delayCall+=FinalizarEditor;
    }
    static void FinalizarEditor()
    {
        bool ok=SessionState.GetString("MenuII.Prueba","")=="salir";
        SessionState.EraseString("MenuII.Prueba");
        MenuIIVerification.RestorePrefs();AudioListener.volume=1;
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
        if(!ok){File.WriteAllText("VerificacionMenuII/estado.txt","FALLO EN PRUEBAS DEL EDITOR");return;}
        File.AppendAllText("VerificacionMenuII/resultado-editor.txt","\nOK: Salir detiene Play en el Editor.\n");
        BuildWindows();
    }
    [MenuItem("Actividad II/Comprobar menu y crear ejecutable")]
    public static void Probar() => Iniciar();

    static void BuildWindows()
    {
        try
        {
            Directory.CreateDirectory("Builds/MenuII");
            string exe=Path.GetFullPath("Builds/MenuII/Proyecto2_MenuII.exe");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{"Assets/Scenes/Menu.unity","Assets/Scenes/Nivel1.unity"},locationPathName=exe,
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development
            });
            if(result.summary.result!=BuildResult.Succeeded) throw new Exception("Build: "+result.summary.result+"; errores: "+result.summary.totalErrors);
            File.WriteAllText("VerificacionMenuII/build.txt","BUILD_WINDOWS_APROBADO\nTamano: "+result.summary.totalSize+" bytes\n");
            var info=new System.Diagnostics.ProcessStartInfo(exe,"-force-d3d11 -screen-fullscreen 0 -screen-width 1280 -screen-height 720 --verificar-menu-ii --pruebas-salida=\""+Path.GetFullPath("VerificacionMenuII")+"\" -logFile \""+Path.GetFullPath("VerificacionMenuII/player.log")+"\"");
            info.WorkingDirectory=Path.GetDirectoryName(exe);info.WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden;info.UseShellExecute=true;
            player=System.Diagnostics.Process.Start(info);playerStarted=EditorApplication.timeSinceStartup;
            EditorApplication.update+=RevisarPlayer;
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("VerificacionMenuII/error-build.txt",e.ToString());}
    }
    static void RevisarPlayer()
    {
        if(player==null) return;
        if(player.HasExited)
        {
            EditorApplication.update-=RevisarPlayer;
            File.WriteAllText("VerificacionMenuII/estado.txt",player.ExitCode==0?"TERMINADO: EDITOR Y BUILD APROBADOS":"FALLO EN PRUEBAS DEL BUILD: "+player.ExitCode);
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
            EditorApplication.ExecuteMenuItem("Window/General/Game");player.Dispose();player=null;
        }
        else if(EditorApplication.timeSinceStartup-playerStarted>120)
        {
            EditorApplication.update-=RevisarPlayer;
            player.Kill();player.Dispose();player=null;
            File.WriteAllText("VerificacionMenuII/estado.txt","FALLO: tiempo de prueba del ejecutable agotado");
        }
    }
}
