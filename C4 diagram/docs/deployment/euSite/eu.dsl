euSite = deploymentNode "Data Center: EU" {
    description "Deployment site for application services."

    !include articleVM.dsl

    !include observabilityVM.dsl

    !include editingVM.dsl

    !include clientVM.dsl
}
