SELECT "Id",
       "Email",
       "FirstName",
       "SubscribedAt",
       "UnsubscribeToken"
FROM public."NewsletterSubscribers"
LIMIT 1000;