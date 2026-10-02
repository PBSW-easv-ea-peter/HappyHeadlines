
deployment happyHeadlines "Production" "FullDeployment" {
    include euSite
    autolayout tb
}

// Article VM only
deployment happyHeadlines "Production" "ArticleVMDeployment" {
    include articleVM
    autolayout tb
}
