using Utilities;
namespace Procedural
{
    [System.Serializable]
    public struct SidewalkBreak
    {
        public GUID Reason;
        public float Start;
        public float End;

        public SidewalkBreak(GUID reason, float start, float end)
        {
            Reason = reason;
            Start = start;
            End = end;
        }
    }
}