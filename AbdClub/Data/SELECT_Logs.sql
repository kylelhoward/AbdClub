SELECT message,
       message_template,
       level,
       "timestamp",
       exception,
       log_event

FROM public."Logs"
--where message like '%Membership reminder sent via SMTP to "kylelhoward@gmail.com"%'
order by "timestamp" desc;

