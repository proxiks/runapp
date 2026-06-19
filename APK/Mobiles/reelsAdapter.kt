class ReelsAdapter : RecyclerView.Adapter<RecyclerView.ViewHolder>() {
    
    companion object {
        const val TYPE_CONTENT = 1
        const val TYPE_AD = 2
        const val AD_SLOT_POSITION = 5  // 2nd ad appears here
    }
    
    private var items: List<ReelsItem> = emptyList()
    
    override fun getItemViewType(position: Int): Int {
        return if (position == AD_SLOT_POSITION) TYPE_AD else TYPE_CONTENT
    }
    
    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): RecyclerView.ViewHolder {
        return when (viewType) {
            TYPE_AD -> AdViewHolder(
                LayoutInflater.from(parent.context)
                    .inflate(R.layout.item_reels_ad, parent, false)
            )
            else -> ReelsViewHolder(
                LayoutInflater.from(parent.context)
                    .inflate(R.layout.item_reels_content, parent, false)
            )
        }
    }
    
    override fun onBindViewHolder(holder: RecyclerView.ViewHolder, position: Int) {
        when (holder) {
            is AdViewHolder -> holder.bind(items[position] as ReelsItem.Ad)
            is ReelsViewHolder -> holder.bind(items[position] as ReelsItem.Content)
        }
    }
    
    inner class AdViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        private val videoView: PlayerView = view.findViewById(R.id.ad_video)
        private val skipButton: Button = view.findViewById(R.id.btn_skip)
        private val ctaButton: Button = view.findViewById(R.id.btn_cta)
        
        fun bind(ad: ReelsItem.Ad) {
            // Auto-play muted video
            val player = ExoPlayer.Builder(itemView.context).build()
            videoView.player = player
            player.setMediaItem(MediaItem.fromUri(ad.mediaUrl))
            player.playWhenReady = true
            player.volume = 0f
            
            // Skip after 5 seconds
            skipButton.visibility = View.GONE
            ad.skippableAfterSec?.let { delay ->
                skipButton.postDelayed({
                    skipButton.visibility = View.VISIBLE
                    skipButton.setOnClickListener { player.stop() }
                }, (delay * 1000).toLong())
            }
            
            ctaButton.text = ad.ctaText
            ctaButton.setOnClickListener {
                // Track click via Lyfron
                trackAdClick(ad.id)
                openUrl(ad.ctaUrl)
            }
        }
    }
}