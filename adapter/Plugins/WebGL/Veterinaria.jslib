mergeInto(LibraryManager.library,{
    VeterinariaBoxingState:function(json){document.dispatchEvent(new CustomEvent('veterinaria-boxeo',{detail:JSON.parse(UTF8ToString(json))}));},
    VeterinariaClinicalState:function(json){document.dispatchEvent(new CustomEvent('veterinaria-clinical',{detail:JSON.parse(UTF8ToString(json))}));},
    VeterinariaInspectionState:function(mode,target,percent){document.dispatchEvent(new CustomEvent('veterinaria-inspection',{detail:{mode:UTF8ToString(mode),target:UTF8ToString(target),percent:percent}}));},
    VeterinariaUseLargePanorama:function(){return window.matchMedia('(pointer:coarse)').matches?0:1;},
    VeterinariaSceneReady:function(name){document.dispatchEvent(new CustomEvent('veterinaria-scene',{detail:UTF8ToString(name)}));},
    VeterinariaScreenActions:function(names){document.dispatchEvent(new CustomEvent('veterinaria-actions',{detail:UTF8ToString(names)}));},
    VeterinariaScreenMode:function(flat){document.documentElement.dataset.mode=flat?'screen':'xr';}
});
