using UnityEngine;

public partial class FrotisExtendidoSuave {
    bool recorridoGuiado;
    Transform laminaGuiada;
    Rigidbody cuerpoGuiado;
    bool eraCinematico;
    Pose poseInicial;
    Vector3 centroLocal, ejeLargo, normal, semiejes;
    public bool ListaParaExtender => !requiereGota || _gotaPuesta || (gotaDeSangre && gotaDeSangre.activeInHierarchy);
    public float FraccionExtendida => yaOculto ? 1f : Mathf.InverseLerp(escalaInicialX, escalaFinalX, mascara.localScale.x);
    public bool Terminada => yaOculto;

    public bool ApoyarLamina(float grados){
        if(recorridoGuiado || yaOculto || !ListaParaExtender || grados<30 || grados>45 || !deslizador)return false;
        var mesh=deslizador.GetComponentInChildren<MeshFilter>();
        if(!mesh || !mesh.sharedMesh)return false;
        laminaGuiada=deslizador;
        poseInicial=new Pose(laminaGuiada.position,laminaGuiada.rotation);
        cuerpoGuiado=laminaGuiada.GetComponent<Rigidbody>();
        if(cuerpoGuiado){eraCinematico=cuerpoGuiado.isKinematic;cuerpoGuiado.isKinematic=true;}
        var bounds=mesh.sharedMesh.bounds;
        centroLocal=laminaGuiada.InverseTransformPoint(mesh.transform.TransformPoint(bounds.center));
        var dims=bounds.extents;
        var axes=new[]{Vector3.right,Vector3.up,Vector3.forward};
        int largo=0,fino=0;
        for(int i=0;i<3;i++){
            semiejes[i]=mesh.transform.TransformVector(axes[i]*dims[i]).magnitude;
            if(semiejes[i]>semiejes[largo])largo=i;
            if(semiejes[i]<semiejes[fino])fino=i;
        }
        ejeLargo=mesh.transform.TransformDirection(axes[largo]);
        normal=mesh.transform.TransformDirection(axes[fino]);
        var plana=Quaternion.LookRotation(ejeLargo,normal);
        var inclinada=Quaternion.AngleAxis(grados,Vector3.forward)*Quaternion.LookRotation(Vector3.right,Vector3.up);
        laminaGuiada.rotation=inclinada*Quaternion.Inverse(plana)*poseInicial.rotation;
        // Register the leading edge against the surface. The authored mask measures travel in world X.
        var contacto=CajaDeLaBanda().center;
        var punta=Quaternion.AngleAxis(grados,Vector3.forward)*new Vector3(semiejes[largo],semiejes[fino],0);
        laminaGuiada.position+=contacto+punta-laminaGuiada.TransformPoint(centroLocal);
        origenGuia=laminaGuiada.position;
        recorridoGuiado=true;_gotaPuesta=true;Soltar();
        MoverLamina(FraccionExtendida);
        return true;
    }
    Vector3 origenGuia;
    public void MoverLamina(float fraccion){
        if(!recorridoGuiado || !laminaGuiada)return;
        float distancia=Mathf.Clamp01(fraccion)*distanciaMaxima;
        laminaGuiada.position=origenGuia+Vector3.right*distancia;
        if(!yaOculto)AplicarDesplazamiento(distancia);
    }
    public void SoltarLamina(){
        if(!recorridoGuiado)return;
        recorridoGuiado=false;
        if(laminaGuiada)laminaGuiada.SetPositionAndRotation(poseInicial.position,poseInicial.rotation);
        if(cuerpoGuiado)cuerpoGuiado.isKinematic=eraCinematico;
        laminaGuiada=null;Soltar();
    }
}