SELECT "Id",
       "Email",
       "FirstName",
       "SubscribedAt",
       "UnsubscribeToken"
FROM public."NewsletterSubscribers"
-- where "FirstName" = 'kyle howard';

-- TRUNCATE TABLE public."NewsletterSubscribers" RESTART IDENTITY CASCADE;
