using System.Collections.Generic;

namespace RZScreenSaver;

public class FolderCollectionSet : List<FolderCollection>{
    public FolderCollection Default{
        get{
            if (Count == 0){
                // add default
                Add(new FolderCollection());
                selectedIndex = 0;
            }
            return this[selectedIndex];
        }
    }
    public int SelectedIndex { get; set; }
    public FolderCollectionSet Clone(){
        return (FolderCollectionSet) MemberwiseClone();
    }
    int selectedIndex;
}