namespace DAL.Entities.Common;


public enum RoomStatus
{
    Available,
    Occupied,
    Maintenance,
    Hidden,
    Disabled
}

public enum TicketStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Closed = 3
}

public enum ContractStatus
{
    Draft = 1,
    Active = 2,
    Expired = 3,
    Terminated = 9
}

public enum BillStatus
{
    Draft = 1,
    Issued = 2,
    Overdue = 3,
    Paid = 4,
    Cancelled = 9
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}

    public enum TicketCategory
    {
        Other = 0,
        Electricity = 1,
        Water = 2,
        Internet = 3,
        Maintenance = 4
    }
public enum SourceType
{
    Manual = 0,
    Billing = 1,
    Contract = 2,
    Maintenance = 3,
    System = 4
}

