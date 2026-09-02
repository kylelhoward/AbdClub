SELECT "Id",
       "Email",
       "FirstName",
       "SubscribedAt",
       "UnsubscribeToken"
FROM public."NewsletterSubscribers"
where "FirstName" = 'Kyle';

-- TRUNCATE TABLE public."NewsletterSubscribers" RESTART IDENTITY CASCADE;
