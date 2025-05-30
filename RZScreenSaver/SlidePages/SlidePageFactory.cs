using System.Diagnostics;

namespace RZScreenSaver.SlidePages{
    static class SlidePageFactory{
        public static ICreator Create(SaverMode mode){
            ICreator creator;
            switch (mode){
            case SaverMode.SlideShow:
                creator = new SimpleSlideCreator();
                break;
            case SaverMode.PhotoCollage:
                creator = new PhotoCollageCreator();
                break;
            case SaverMode.Mixed:
                creator = new MixedCreator();
                break;
            default:
                creator = new MixedCreator();
                Trace.WriteLine("SlidePageFactory: " + mode + " is not handled!!");
                break;
            }
            return creator;
        }
        public interface ICreator{
            ISlidePage Create(DisplayMode displayMode);
        }

        class SimpleSlideCreator : ICreator{
            public ISlidePage Create(DisplayMode displayMode)
                => new SimpleSlide{DisplayMode = displayMode};
        }

        class PhotoCollageCreator : ICreator{
            public ISlidePage Create(DisplayMode displayMode)
                => new PhotoCollagePage{DisplayMode = displayMode};
        }

        class MixedCreator : ICreator{
            public ISlidePage Create(DisplayMode displayMode){
                if (++count % 2 == 0)
                    return new SimpleSlide{DisplayMode = displayMode};
                else
                    return new PhotoCollagePage{DisplayMode = displayMode};
            }
            int count;
        }
    }
}