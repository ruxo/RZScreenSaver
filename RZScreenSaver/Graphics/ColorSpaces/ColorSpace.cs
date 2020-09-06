namespace RZScreenSaver.Graphics.ColorSpaces{
    public abstract class ColorSpace<T> where T : struct{
        protected ColorSpace(int width, int height, int dataPerPixel){
            Width = width;
            Height = height;
            DataPerPixel = dataPerPixel;
            data = new T[Area * DataPerPixel];
        }
        public int Area{
            get { return Width*Height; }
        }
        public T[] Data{
            get { return data; }
        }
        public int Width { get; private set; }
        public int Height { get; private set; }
        protected int DataPerPixel { get; private set; }
        readonly T[] data;
    }
}