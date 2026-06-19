class VerifiedBadgeActivity : AppCompatActivity() {
    
    private lateinit var billingClient: BillingClient
    private val VERIFIED_SKU = "jatinbook_verified_monthly"
    
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_verified_badge)
        
        setupBilling()
        
        findViewById<Button>(R.id.btnSubscribe).setOnClickListener {
            launchPurchaseFlow()
        }
    }
    
    private fun setupBilling() {
        billingClient = BillingClient.newBuilder(this)
            .setListener { billingResult, purchases ->
                if (billingResult.responseCode == BillingResponseCode.OK) {
                    purchases?.forEach { purchase ->
                        verifyWithLyfron(purchase.purchaseToken)
                    }
                }
            }
            .enablePendingPurchases()
            .build()
        
        billingClient.startConnection(object : BillingClientStateListener {
            override fun onBillingSetupFinished(result: BillingResult) {
                if (result.responseCode == BillingResponseCode.OK) {
                    querySkuDetails()
                }
            }
            override fun onBillingServiceDisconnected() {}
        })
    }
    
    private fun querySkuDetails() {
        val params = SkuDetailsParams.newBuilder()
            .setSkusList(listOf(VERIFIED_SKU))
            .setType(BillingClient.SkuType.SUBS)
            .build()
        
        billingClient.querySkuDetailsAsync(params) { result, skuList ->
            skuList?.find { it.sku == VERIFIED_SKU }?.let { sku ->
                findViewById<TextView>(R.id.tvPrice).text = sku.price
            }
        }
    }
    
    private fun launchPurchaseFlow() {
        val flowParams = BillingFlowParams.newBuilder()
            .setSkuDetails(skuDetails)
            .build()
        billingClient.launchBillingFlow(this, flowParams)
    }
    
    private fun verifyWithLyfron(token: String) {
        // Hit your Lyfron backend to validate + activate
        lifecycleScope.launch {
            val response = lyfronApi.verifyPurchase(
                userId = getUserId(),
                purchaseToken = token,
                sku = VERIFIED_SKU
            )
            if (response.success) {
                saveVerifiedStatus(true)
                showSuccessAnimation()
            }
        }
    }
}