using System.Collections.Generic;

namespace RZScreenSaver{
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
        public int SelectedIndex{
            get { return selectedIndex; }
            set { selectedIndex = (value < 0 || value >= Count) ? 0 : value; }
        }
        public FolderCollectionSet Clone(){
            return (FolderCollectionSet) MemberwiseClone();
        }
        int selectedIndex;
    }
}