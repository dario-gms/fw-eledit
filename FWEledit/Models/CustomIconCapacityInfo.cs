namespace FWEledit
{
    public sealed class CustomIconCapacityInfo
    {
        public int IconWidth { get; set; }

        public int IconHeight { get; set; }

        public int Rows { get; set; }

        public int Columns { get; set; }

        public int MaxRows { get; set; }

        public int UsedSlots { get; set; }

        public int Capacity
        {
            get { return Rows * Columns; }
        }

        public int MaxCapacity
        {
            get { return MaxRows * Columns; }
        }

        public int FreeSlots
        {
            get { return Capacity - UsedSlots; }
        }

        public int ExpandableSlots
        {
            get { return MaxCapacity - Capacity; }
        }

        public bool CanExpand
        {
            get { return ExpandableSlots > 0; }
        }
    }
}
