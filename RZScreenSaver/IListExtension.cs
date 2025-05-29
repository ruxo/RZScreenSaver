using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace RZScreenSaver;

static class IListExtension{
    public static T[] CastToArray<T>(this IList list){
        var result = new T[list.Count];
        for(var i=0; i < result.Length; ++i)
            result[i] = (T) list[i];
        return result;
    }
    public static int IndexOf<T>(this IList list, Func<T,bool> predicate){
        for(var i=0; i < list.Count; ++i){
            if (predicate((T)list[i]))
                return i;
        }
        return -1;
    }
    public static void Shuffle<T>(this T[] source, int pos1, int pos2, ref T[] target){
        Debug.Assert(source.Length == target.Length);
        var firstCut = Math.Min(pos1, pos2);
        var secondCut = Math.Max(pos1, pos2);
        var firstChunkLength = firstCut;
        var secondChunkLength = secondCut - firstCut;
        var thirdChunkLength = source.Length - secondCut;
        const int targetFirstChunk = 0;
        if (pos1 < pos2){
            // perform shuffle pattern 1: swap chunk #1 and #2.
            var targetSecondChunk = secondChunkLength;
            var targetThirdChunk = secondChunkLength + firstChunkLength;
            Array.Copy(source, 0, target, targetSecondChunk, firstChunkLength);
            Array.Copy(source, firstCut, target, targetFirstChunk, secondChunkLength);
            Array.Copy(source, secondCut, target, targetThirdChunk, thirdChunkLength);
        }else{
            // swap chunk #1 and #3
            var targetSecondChunk = thirdChunkLength;
            var targetThirdChunk = thirdChunkLength + secondChunkLength;
            Array.Copy(source, 0, target, targetThirdChunk, firstChunkLength);
            Array.Copy(source, firstCut, target, targetSecondChunk, secondChunkLength);
            Array.Copy(source, secondCut, target, targetFirstChunk, thirdChunkLength);
        }
    }
    public static void Swap<T>(this IList<T> list, int pos1, int pos2){
        (list[pos1], list[pos2]) = (list[pos2], list[pos1]);
    }
}