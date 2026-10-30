namespace Gise.Exceptions;

public static class ErrorCodes
{
    public static class General
    {
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string InternalError = "INTERNAL_SERVER_ERROR";
        public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";

        // Servisler değil APISIX üretir (deploy/apisix/seed.sh): istek sınırı aşıldı, tanımsız API yolu ya da yöntemi.
        public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
        public const string RouteNotFound = "ROUTE_NOT_FOUND";
    }

    public static class Auth
    {
        public const string MissingClaim = "AUTH_MISSING_CLAIM";
    }

    public static class Venue
    {
        public const string NotFound = "VENUE_NOT_FOUND";
        public const string HasEvents = "VENUE_HAS_EVENTS";
    }

    public static class Event
    {
        public const string NotFound = "EVENT_NOT_FOUND";
        public const string NotOwner = "EVENT_NOT_OWNER";
        public const string Cancelled = "EVENT_CANCELLED";
        public const string AlreadyPublished = "EVENT_ALREADY_PUBLISHED";
        public const string NotPublished = "EVENT_NOT_PUBLISHED";
        public const string AlreadyStarted = "EVENT_ALREADY_STARTED";
        public const string NotDraft = "EVENT_NOT_DRAFT";
        public const string VenueLocked = "EVENT_VENUE_LOCKED";
        public const string InReview = "EVENT_IN_REVIEW";
        public const string NotInReview = "EVENT_NOT_IN_REVIEW";
        public const string AdminOnly = "EVENT_ADMIN_ONLY";
    }

    public static class Media
    {
        public const string TypeNotAllowed = "MEDIA_TYPE_NOT_ALLOWED";
        public const string TooLarge = "MEDIA_TOO_LARGE";
        public const string NotFound = "MEDIA_NOT_FOUND";
        public const string CorruptContent = "MEDIA_CORRUPT_CONTENT";
    }

    public static class Stock
    {
        public const string NotFound = "STOCK_NOT_FOUND";
        public const string NotOnSale = "STOCK_NOT_ON_SALE";
    }

    public static class Hold
    {
        public const string SeatsUnavailable = "HOLD_SEATS_UNAVAILABLE";
        public const string SeatNotInEvent = "HOLD_SEAT_NOT_IN_EVENT";
        public const string AlreadyActive = "HOLD_ALREADY_ACTIVE";
        public const string OrderClosed = "HOLD_ORDER_CLOSED";
        public const string NotActive = "HOLD_NOT_ACTIVE";
    }

    public static class Order
    {
        public const string NotFound = "ORDER_NOT_FOUND";
        public const string NotOwner = "ORDER_NOT_OWNER";
        public const string NotCancellable = "ORDER_NOT_CANCELLABLE";
        public const string NotPayable = "ORDER_NOT_PAYABLE";
    }

    public static class Ticket
    {
        public const string NotFound = "TICKET_NOT_FOUND";
        public const string NotOwner = "TICKET_NOT_OWNER";
        public const string AlreadyUsed = "TICKET_ALREADY_USED";
        public const string Cancelled = "TICKET_CANCELLED";
        public const string SeatTaken = "TICKET_SEAT_TAKEN";
        public const string EventCancelled = "TICKET_EVENT_CANCELLED";
    }

    public static class Price
    {
        public const string SectionNotInVenue = "PRICE_SECTION_NOT_IN_VENUE";
        public const string MissingSections = "PRICE_MISSING_SECTIONS";
    }
}
