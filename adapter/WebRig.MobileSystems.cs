using OseoVR;
using UnityEngine;

public sealed partial class WebRig {
    public PanelSistemaCaninoController mobileSystems;
    public ArranqueSistemasConBoton mobileStart;
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void VeterinariaSystems(string names);
    void ShowMobileSystems(bool show) {
#if UNITY_WEBGL && !UNITY_EDITOR
        VeterinariaSystems(show && mobileSystems?string.Join("|",mobileSystems.sistemas.ConvertAll(s=>s.nombre)):"");
#endif
    }
    void UpdateMobileStart(){
        if(!mobileStart || !mobileStart.botonIniciar)return;
        // Reiniciar targets the full list. Iniciar uses only the selected entry on a phone.
        if(!mobileStart.botonIniciar.activeSelf){if(mobileStart.soloEstos.Count>0)mobileStart.soloEstos.Clear();}
        else if(mobileStart.soloEstos.Count==0){
            var first=mobileSystems.sistemas.Find(s=>s.objeto && s.nombre=="Oseo");
            if(first!=null)mobileStart.soloEstos.Add(first.objeto);
        }
    }
    public void SelectMobileSystem(string value){
        if(!OnScreen || !mobileSystems || !int.TryParse(value,out var i) || i<0 || i>=mobileSystems.sistemas.Count)return;
        var system=mobileSystems.sistemas[i].objeto;
        if(!system)return;
        if(mobileStart && mobileStart.botonIniciar && mobileStart.botonIniciar.activeSelf){
            mobileStart.soloEstos.Clear();mobileStart.soloEstos.Add(system);
            mobileStart.Iniciar();
        }else mobileSystems.MostrarSistema(i);
    }
}
