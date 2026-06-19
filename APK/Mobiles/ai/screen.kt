package com.jatin.runapp.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.navigation.NavHostController
import com.jatin.runapp.data.MemeRepository
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MemeGeneratorScreen(navController: NavHostController) {
    val repository = remember { MemeRepository() }
    
    var topic by remember { mutableStateOf("") }
    var generatedMeme by remember { mutableStateOf<GeneratedMeme?>(null) }
    var isGenerating by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf("") }
    var selectedTemplate by remember { mutableStateOf(memeTemplates[0]) }
    
    val scope = rememberCoroutineScope()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("🤖 AI Meme Generator") },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .padding(16.dp)
        ) {
            // Topic Input
            OutlinedTextField(
                value = topic,
                onValueChange = { 
                    topic = it
                    error = ""
                },
                label = { Text("What's the meme about?") },
                placeholder = { Text("e.g., coding at 3am, Monday vibes...") },
                leadingIcon = { Icon(Icons.Default.EmojiEmotions, null) },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )

            Spacer(modifier = Modifier.height(20.dp))

            // Template Selector
            Text(
                text = "Pick a Template",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
            )
            
            Spacer(modifier = Modifier.height(8.dp))

            LazyRow(
                horizontalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                items(memeTemplates) { template ->
                    TemplateCard(
                        template = template,
                        selected = selectedTemplate == template,
                        onClick = { selectedTemplate = template }
                    )
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            // Generate Button
            Button(
                onClick = {
                    if (topic.isBlank()) {
                        error = "Enter a topic first!"
                        return@Button
                    }
                    isGenerating = true
                    error = ""
                    scope.launch {
                        val result = repository.generateMemeText(topic, selectedTemplate.id)
                        isGenerating = false
                        result.onSuccess { (top, bottom) ->
                            generatedMeme = GeneratedMeme(
                                template = selectedTemplate,
                                topText = top,
                                bottomText = bottom,
                                topic = topic
                            )
                        }.onFailure {
                            error = it.message ?: "Failed to generate meme"
                        }
                    }
                },
                modifier = Modifier
                    .fillMaxWidth()
                    .height(56.dp),
                enabled = !isGenerating && topic.isNotBlank()
            ) {
                if (isGenerating) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(24.dp),
                        color = MaterialTheme.colorScheme.onPrimary
                    )
                    Spacer(modifier = Modifier.width(12.dp))
                    Text("Grok is thinking...")
                } else {
                    Icon(Icons.Default.AutoAwesome, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Generate Meme", fontSize = 18.sp)
                }
            }

            if (error.isNotEmpty()) {
                Spacer(modifier = Modifier.height(12.dp))
                Card(
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.errorContainer
                    )
                ) {
                    Text(
                        text = error,
                        color = MaterialTheme.colorScheme.onErrorContainer,
                        modifier = Modifier.padding(12.dp)
                    )
                }
            }

            // Generated Meme Preview
            generatedMeme?.let { meme ->
                Spacer(modifier = Modifier.height(24.dp))
                
                Text(
                    text = "Your Meme",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold
                )
                
                Spacer(modifier = Modifier.height(12.dp))

                MemePreviewCard(meme = meme)

                Spacer(modifier = Modifier.height(16.dp))

                // Action Buttons
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    OutlinedButton(
                        onClick = {
                            isGenerating = true
                            scope.launch {
                                val result = repository.generateMemeText(topic, selectedTemplate.id)
                                isGenerating = false
                                result.onSuccess { (top, bottom) ->
                                    generatedMeme = meme.copy(topText = top, bottomText = bottom)
                                }
                            }
                        },
                        modifier = Modifier.weight(1f),
                        enabled = !isGenerating
                    ) {
                        Icon(Icons.Default.Refresh, contentDescription = null)
                        Spacer(modifier = Modifier.width(4.dp))
                        Text("Regenerate")
                    }

                    Button(
                        onClick = { /* Share to JatinBook feed */ },
                        modifier = Modifier.weight(1f)
                    ) {
                        Icon(Icons.Default.Share, contentDescription = null)
                        Spacer(modifier = Modifier.width(4.dp))
                        Text("Post to Feed")
                    }
                }

                Spacer(modifier = Modifier.height(8.dp))

                OutlinedButton(
                    onClick = { 
                        topic = ""
                        generatedMeme = null
                        error = ""
                    },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.Clear, contentDescription = null)
                    Spacer(modifier = Modifier.width(4.dp))
                    Text("Start Over")
                }
            }
        }
    }
}

// ========== DATA CLASSES ==========

data class MemeTemplate(
    val id: String,
    val name: String,
    val emoji: String
)

data class GeneratedMeme(
    val template: MemeTemplate,
    val topText: String,
    val bottomText: String,
    val topic: String
)

val memeTemplates = listOf(
    MemeTemplate("drake", "Drake Hotline Bling", "🙅"),
    MemeTemplate("distracted", "Distracted Boyfriend", "👀"),
    MemeTemplate("change_mind", "Change My Mind", "☕"),
    MemeTemplate("always_has", "Always Has Been", "🌍"),
    MemeTemplate("stonks", "Stonks", "📈"),
    MemeTemplate("programmer", "Programmer", "💻"),
    MemeTemplate("cat", "Woman Yelling at Cat", "🐱"),
    MemeTemplate("scroll", "Scroll of Truth", "📜")
)

// ========== UI COMPONENTS ==========

@Composable
fun TemplateCard(template: MemeTemplate, selected: Boolean, onClick: () -> Unit) {
    Card(
        onClick = onClick,
        modifier = Modifier.width(100.dp),
        colors = CardDefaults.cardColors(
            containerColor = if (selected) 
                MaterialTheme.colorScheme.primaryContainer 
            else 
                MaterialTheme.colorScheme.surfaceVariant
        ),
        border = if (selected) {
            androidx.compose.foundation.BorderStroke(2.dp, MaterialTheme.colorScheme.primary)
        } else null
    ) {
        Column(
            modifier = Modifier.padding(12.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                text = template.emoji,
                fontSize = 32.sp
            )
            Spacer(modifier = Modifier.height(4.dp))
            Text(
                text = template.name,
                style = MaterialTheme.typography.labelSmall,
                textAlign = TextAlign.Center,
                maxLines = 2
            )
        }
    }
}

@Composable
fun MemePreviewCard(meme: GeneratedMeme) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        elevation = CardDefaults.cardElevation(defaultElevation = 8.dp)
    ) {
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .height(350.dp)
                .background(Color.DarkGray)
                .padding(16.dp)
        ) {
            // Meme Image Placeholder
            Column(
                modifier = Modifier.fillMaxSize(),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.SpaceBetween
            ) {
                // Top Text
                Text(
                    text = meme.topText.uppercase(),
                    fontSize = 24.sp,
                    fontWeight = FontWeight.ExtraBold,
                    color = Color.White,
                    textAlign = TextAlign.Center,
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(Color.Black.copy(alpha = 0.5f))
                        .padding(8.dp)
                )

                // Center - Template emoji
                Text(
                    text = meme.template.emoji,
                    fontSize = 80.sp
                )

                // Bottom Text
                Text(
                    text = meme.bottomText.uppercase(),
                    fontSize = 24.sp,
                    fontWeight = FontWeight.ExtraBold,
                    color = Color.White,
                    textAlign = TextAlign.Center,
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(Color.Black.copy(alpha = 0.5f))
                        .padding(8.dp)
                )
            }

            // Watermark
            Text(
                text = "runapp AI",
                fontSize = 10.sp,
                color = Color.White.copy(alpha = 0.5f),
                modifier = Modifier.align(Alignment.BottomEnd)
            )
        }
    }
}
