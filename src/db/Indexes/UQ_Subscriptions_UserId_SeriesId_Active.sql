-- One active subscription per user per series (filtered: logical-delete allows re-subscribe)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Subscriptions_UserId_SeriesId_Active]
ON [dbo].[Subscriptions] ([UserId], [SeriesId])
WHERE [IsDeleted] = 0;
