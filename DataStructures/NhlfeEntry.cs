
namespace DataStructures
{
    public struct NhlfeEntry
    {
        public string operation;
        public int? outLabel;
        public int? outPort;
        public int? nextId;

        public NhlfeEntry(string operation, int? outLabel, int? outPort, int? nextId)
        {
            this.operation = operation;
            this.outLabel = outLabel;
            this.outPort = outPort;
            this.nextId = nextId;
        }
    }
}