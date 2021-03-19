using Cloo;

namespace CL
{
    public static class Extensions
    {
        public static void Dispose(this ComputeEventList eventList)
        {
            foreach (ComputeEventBase eventBase in eventList)
                eventBase.Dispose();
            eventList.Clear();
        }
    }
}
