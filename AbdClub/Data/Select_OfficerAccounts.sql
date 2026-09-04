SELECT o."Id",
       o."Email",
       o."GoogleSubId",
       o."AccessLevel",
       o."OfficerTitle",
       o."IsEnabled",
       o."MemberId",
       o."CreatedAt"
FROM public."OfficerAccounts" as "o"
inner join public."Members" as "m"
on "o"."MemberId" = "m"."Id"
where m."Id"
 in (
47,
23,
52,
84,
37,
109
);

SELECT 
*
FROM public."Members"
where "Id"
 in (
47,
23,
52,
84,
37,
109
)