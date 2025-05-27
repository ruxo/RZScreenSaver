using System;
using System.Collections;
using System.Collections.Generic;

namespace RZScreenSaver;

public enum InclusionMode{
    Single,
    Recursive,
    Exclude,
}
public sealed class FolderInclusion(string path, InclusionMode inclusion)
{
    public string Path => path;

    public InclusionMode Inclusion => inclusion;

    public override bool Equals(object? obj) {
        var another = obj as FolderInclusion;
        return another != null && Path.Equals(another.Path, StringComparison.OrdinalIgnoreCase);
    }
    public override int GetHashCode() => Path.GetHashCode();
}

public sealed class FolderCollection : ICollection<FolderInclusion>{
    public int Count
        => paths.Count;

    public bool IsReadOnly => false;

    public FolderInclusion Add(string path, InclusionMode mode){
        var folder = new FolderInclusion(path,mode);
        Add(folder);
        return folder;
    }
    public void Add(FolderInclusion folder){
        var index = paths.IndexOf(folder);
        if (index == -1)
            paths.Add(folder);
        else
            paths[index] = folder;
    }
    public void Clear(){
        paths.Clear();
    }

    public bool Contains(FolderInclusion item)
        => paths.Contains(item);

    public void CopyTo(FolderInclusion[] array, int arrayIndex) {
        paths.CopyTo(array, arrayIndex);
    }

    public bool Remove(FolderInclusion folder)
        => paths.Remove(folder);

    public bool Contains(string path){
        return paths.Exists(folder => folder.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    #region Implementation of IEnumerable

    public IEnumerator<FolderInclusion> GetEnumerator(){
        return paths.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator(){
        return GetEnumerator();
    }

    #endregion

    readonly List<FolderInclusion> paths = new();
}