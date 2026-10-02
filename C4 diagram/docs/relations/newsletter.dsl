
// newsletterService -> articleService "Retrieves articles"
newsletterService -> articleService "Retrieves articles"
newsletterService -> subscriberService "Retrieves subscribers"
newsletterService -> subscriberQueue "Consumes new subscribers"
newsletterService -> reader "Sends daily newsletter"
