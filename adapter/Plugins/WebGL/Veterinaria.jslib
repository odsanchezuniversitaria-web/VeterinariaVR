mergeInto(LibraryManager.library,{
    VeterinariaSceneReady:function(name){document.dispatchEvent(new CustomEvent('veterinaria-scene',{detail:UTF8ToString(name)}));},
    VeterinariaScreenActions:function(names){document.dispatchEvent(new CustomEvent('veterinaria-actions',{detail:UTF8ToString(names)}));},
    VeterinariaScreenMode:function(flat){document.documentElement.dataset.mode=flat?'screen':'xr';}
});
