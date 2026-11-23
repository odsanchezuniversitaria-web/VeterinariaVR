using System;
using System.Runtime.InteropServices;
using OseoVR.Boxeo;
using UnityEngine;

public sealed partial class WebRig {
    [Serializable]
    class BoxeoPantalla {
        public string estado, objetivo, aviso;
        public int segundos, puntos, ronda, racha;
    }

    ControladorJuego juegoBoxeo;
    BotonInicioBoxeo placaInicio;
    PausaBoxeo pausaBoxeo;
    string boxeoPublicado;
    float proximoDatoBoxeo;
    [DllImport("__Internal")] static extern void VeterinariaBoxingState(string json);

    void EntrarBoxeoPantalla(){
        if(gameObject.scene.name!="4ScenaBoxeo")return;
        juegoBoxeo=FindFirstObjectByType<ControladorJuego>();
        placaInicio=FindFirstObjectByType<BotonInicioBoxeo>();
        pausaBoxeo=FindFirstObjectByType<PausaBoxeo>();
        boxeoPublicado=null;
        proximoDatoBoxeo=0;
    }

    void SalirBoxeoPantalla(){
        if(!juegoBoxeo)return;
        juegoBoxeo=null;
        placaInicio=null;
        pausaBoxeo=null;
#if UNITY_WEBGL && !UNITY_EDITOR
        VeterinariaBoxingState("{}");
#endif
    }

    public void BoxeoAccion(string accion){
        if(!OnScreen || !juegoBoxeo)return;
        switch(accion){
            case "inicio": if(placaInicio && placaInicio.isActiveAndEnabled)placaInicio.Pulsar();break;
            case "pausa": if(juegoBoxeo.JuegoActivo && pausaBoxeo)pausaBoxeo.Alternar();break;
            case "repetir": if(juegoBoxeo.TiempoRestante<=0 && pausaBoxeo)pausaBoxeo.Reiniciar();break;
        }
    }

    bool IntentarBoxeoPantalla(Collider hit){
        if(!juegoBoxeo || !OnScreen)return false;
        var hueso=hit.GetComponentInParent<HuesoInteractivo>();
        if(hueso){hueso.GolpearEnPantalla();return true;}
        var inicio=hit.GetComponentInParent<BotonInicioBoxeo>();
        if(!inicio)return false;
        if(inicio.isActiveAndEnabled)inicio.Pulsar();
        return true;
    }

    void ActualizarBoxeoPantalla(){
#if UNITY_WEBGL && !UNITY_EDITOR
        if(!OnScreen || !juegoBoxeo || Time.unscaledTime<proximoDatoBoxeo)return;
        proximoDatoBoxeo=Time.unscaledTime+.13f;
        var g=juegoBoxeo.generador;
        string estado=juegoBoxeo.JuegoActivo?(Time.timeScale==0?"pausa":"jugando"):
            juegoBoxeo.TiempoRestante<=0?"fin":placaInicio && placaInicio.isActiveAndEnabled?"inicio":"cuenta";
        var datos=new BoxeoPantalla{
            estado=estado,objetivo=g && !string.IsNullOrEmpty(g.nombreHuesoObjetivo)?NombresHuesos.Bonito(g.nombreHuesoObjetivo):"",
            aviso=juegoBoxeo.textoAviso?juegoBoxeo.textoAviso.text.Trim():"",
            puntos=juegoBoxeo.Puntaje,racha=juegoBoxeo.Racha,ronda=g?g.RondaActual:0,
            segundos=Mathf.CeilToInt(juegoBoxeo.TiempoRestante)
        };
        string json=JsonUtility.ToJson(datos);
        if(json!=boxeoPublicado){boxeoPublicado=json;VeterinariaBoxingState(json);}
#endif
    }
}
