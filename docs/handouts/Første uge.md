---
course: Development of Large Systems
type: semester-project
week: 35
company: Happy Headlines
---
## The system

The HappyHeadlines system is composed of several containers that work together to support a positive news website. There are two main users of the system: the **Publisher** and the **Reader**. The Publisher writes articles for HappyHeadlines using the **Webapp**, which allows them to save drafts and publish finished articles. Drafts are managed through the **DraftService**, which stores and retrieves drafts from the **DraftDatabase**. When an article is ready to be published, the Webapp interacts with the **PublisherService**, which is responsible for finalising the publication. Before publishing, the PublisherService consults the **ProfanityService** to filter out inappropriate language using the **ProfanityDatabase**. Once approved, the article is placed into the **ArticleQueue**, from which it is later stored in the **ArticleDatabase**.

The Reader accesses the **Website**, which displays the most recent articles and highlights one article in particular. The Website fetches article data from the **ArticleService**, which retrieves content from the ArticleDatabase and subscribes to new articles appearing in the ArticleQueue. Readers can post comments on articles via the Website, which passes them to the **CommentService**. This service filters out profanity using the ProfanityService and stores or retrieves comments through the **CommentDatabase**.

Readers can also subscribe to a daily newsletter. Subscription requests made through the Website are handled by the **SubscriberService**, which stores subscriber information in the **SubscriberDatabase** and places new subscribers into the **SubscriberQueue**. The **NewsletterService** manages the process of sending newsletters to subscribers. It retrieves recent articles from the ArticleService and subscriber information from the SubscriberService before sending the newsletter to all active subscribers.

The ProfanityService plays a key role in both publishing and commenting workflows by interacting with the ProfanityDatabase to retrieve or remove prohibited words. The ArticleService ensures that published articles are correctly delivered to both the Website and the NewsletterService. All queues and databases, including the ArticleQueue, SubscriberQueue, DraftDatabase, ArticleDatabase, CommentDatabase, ProfanityDatabase, and SubscriberDatabase, support the flow of data and ensure persistence and decoupling between services. The entire system operates as a coordinated set of containers that manage drafting, publishing, reading, commenting, and subscribing functionalities for a smooth and positive user experience.

## Your project

This first week you are not going to implement anything, but you are going to draw a C4 diagram based on the following description of the system. You are expected to complete the context- and the container-level (the first two levels) of the application.