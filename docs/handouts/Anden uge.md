---
course: Development of Large Systems
type: semester-project
week: 36
company: Happy Headlines
---

# Krav for gennemførelse

Implement the ArticleService and the ArticleDatabase. The ArticleService needs four endpoints for Create, Read, Update and Delete. It must be implemented as a REST-based API.

You have to perform a x-axis split on the ArticleService so that three instances are available with a load balancer to take care of which instance will service the incoming request.

Furthermore you have to perform a z-axis split on the ArticleDatabase such that each continent has their own database. Along with the seven continent database and eighth database will contain news articles that’s relevant across the entire world - a global database so-to-speak.
