using System;
using System.Drawing;

namespace RZScreenSaver.SlidePages;

static class SlidePageFactory{
    public static Func<Size,DisplayMode,ISlidePage> Create(SaverMode mode) {
        return mode switch {
            SaverMode.SlideShow    => (_, displayMode) => new SimpleSlide { DisplayMode = displayMode },
            SaverMode.PhotoCollage => (size, displayMode) => new PhotoCollagePage(size) { DisplayMode = displayMode },

            _ => new MixedCreator().Create,
        };
    }

    sealed class MixedCreator
    {
        public ISlidePage Create(Size size, DisplayMode displayMode) {
            if (++count % 2 == 0)
                return new SimpleSlide { DisplayMode = displayMode };
            else
                return new PhotoCollagePage(size) { DisplayMode = displayMode };
        }

        int count;
    }
}