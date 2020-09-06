using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace RZScreenSaver{
    public enum InclusionMode{
        Single,
        Recursive,
        Exclude,
    }
    public class FolderInclusion{
// ReSharper disable UnusedMember.Global
        // this is needed by XmlSerializer, don't remove!
        public FolderInclusion(){}
// ReSharper restore UnusedMember.Global
        public FolderInclusion(string path, InclusionMode mode){
            Path = path;
            Inclusion = mode;
        }
        public string Path { get; set;}

        public InclusionMode Inclusion { get; set; }

        public override bool Equals(object obj) {
            var another = obj as FolderInclusion;
            return another != null && Path.Equals(another.Path, StringComparison.OrdinalIgnoreCase);
        }
        public override int GetHashCode() {
            return Path == null? 0 : Path.GetHashCode();
        }
    }
    public class FolderCollection : IEnumerable<FolderInclusion>{
        public int Count{
            get { return paths.Count; }
        }
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
        public void Remove(FolderInclusion folder){
            paths.Remove(folder);
        }
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

        readonly List<FolderInclusion> paths = new List<FolderInclusion>();
    }
}