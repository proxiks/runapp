/* amdxdna part of runapp security ai */
    .text
    .globl lyfron_amdxdna_secure
    .globl lyfron_amdxdna_init
    .globl lyfron_amdxdna_inference
    .globl lyfron_amdxdna_model_load
    .globl lyfron_amdxdna_threat_detect
    .globl lyfron_amdxdna_profile_score

/* ─── XDNA NPU Register Map (Ryzen AI) ─── */
.set XDNA_NPU_BASE,          0x15000000
.set XDNA_NPU_CTRL,           0x0000      /* Control register */
.set XDNA_NPU_STATUS,         0x0004      /* Status register */
.set XDNA_NPU_IRQ,            0x0008      /* Interrupt control */
.set XDNA_NPU_PC,             0x0010      /* Program counter */
.set XDNA_NPU_DMA_SRC,        0x0100      /* DMA source address */
.set XDNA_NPU_DMA_DST,        0x0108      /* DMA destination address */
.set XDNA_NPU_DMA_LEN,        0x0110      /* DMA length */
.set XDNA_NPU_DMA_CTRL,       0x0118      /* DMA control */
.set XDNA_NPU_AIE_COL,        0x0200      /* AIE array column select */
.set XDNA_NPU_AIE_ROW,        0x0204      /* AIE array row select */
.set XDNA_NPU_AIE_DATA,       0x0208      /* AIE data register */
.set XDNA_NPU_AIE_INSTR,      0x020C      /* AIE instruction register */

/* ─── Lyfron Security Model Constants ─── */
.set LYFRON_MODEL_MAGIC,      0x4C59464D  /* "LYFM" */
.set LYFRON_MODEL_VERSION,    0x00030000  /* v3.0 */
.set LYFRON_INPUT_FEATURES,   256         /* Feature vector size */
.set LYFRON_HIDDEN_LAYERS,    4           /* Neural network depth */
.set LYFRON_OUTPUT_CLASSES,   16          /* Threat categories */

/* ─── XDNA Initialization ─── */

lyfron_amdxdna_init:
    /* Entry: Initialize AMD XDNA NPU for security inference */
    /* C ABI: void lyfron_amdxdna_init(void) */
    
    pushq %rbp
    movq %rsp, %rbp
    pushq %rbx
    pushq %r12
    pushq %r13
    pushq %r14
    pushq %r15
    
    /* Verify license */
    VERIFY_LICENSE 0x05
    
    /* Map XDNA NPU registers */
    movq $XDNA_NPU_BASE, %rdi
    movq $4096, %rsi            /* Page size */
    movq $3, %rdx               /* PROT_READ | PROT_WRITE */
    movq $0x11, %r10            /* MAP_SHARED | MAP_LOCKED */
    xorq %r8, %r8               /* fd - will be set by kernel */
    xorq %r9, %r9               /* offset */
    
    /* Call kernel mmap (simplified - actual via driver ioctl) */
    call lyfron_map_xdna_registers
    
    testq %rax, %rax
    js .L_xdna_init_fail
    
    movq %rax, %r12             /* Save NPU base in %r12 */
    
    /* Reset NPU */
    movl $0x1, XDNA_NPU_CTRL(%r12)      /* Assert reset */
    movl $0x0, XDNA_NPU_CTRL(%r12)      /* Deassert */
    
    /* Wait for ready */
.L_xdna_wait_ready:
    movl XDNA_NPU_STATUS(%r12), %eax
    testl $0x1, %eax
    jz .L_xdna_wait_ready
    
    /* Load microcode */
    leaq lyfron_xdna_microcode(%rip), %r13
    movq $lyfron_xdna_microcode_end - lyfron_xdna_microcode, %r14
    
    /* Program AIE array with security inference kernels */
    call lyfron_load_aie_security_kernels
    
    /* Initialize DMA buffers for feature vectors */
    call lyfron_init_xdna_dma_buffers
    
    /* Success */
    xorq %rax, %rax
    jmp .L_xdna_init_done
    
.L_xdna_init_fail:
    movq $-1, %rax
    
.L_xdna_init_done:
    popq %r15
    popq %r14
    popq %r13
    popq %r12
    popq %rbx
    popq %rbp
    ret

/* ─── Security Inference Engine ─── */

lyfron_amdxdna_inference:
    /* C ABI: float lyfron_amdxdna_inference(float* features, int count) */
    /* %rdi = feature array, %rsi = count */
    
    pushq %rbp
    movq %rsp, %rbp
    pushq %rbx
    pushq %r12
    pushq %r13
    pushq %r14
    pushq %r15
    
    movq %rdi, %r13             /* Feature array */
    movl %esi, %r14d            /* Count */
    
    /* Load features into NPU via DMA */
    movq %r13, %rdi
    movq lyfron_xdna_input_buf(%rip), %rsi
    movq %r14, %rdx
    call lyfron_xdna_dma_to_npu
    
    /* Trigger inference */
    movl $0x1, XDNA_NPU_CTRL(%r12)      /* Start computation */
    
    /* Poll for completion (or use interrupt in production) */
.L_xdna_inference_wait:
    movl XDNA_NPU_STATUS(%r12), %eax
    testl $0x2, %eax                    /* Done bit */
    jz .L_xdna_inference_wait
    
    /* Read result from NPU */
    movq lyfron_xdna_output_buf(%rip), %rdi
    movq %r15, %rsi                     /* Result buffer */
    movq $16, %rdx                      /* 16 output classes */
    call lyfron_xdna_dma_from_npu
    
    /* Extract threat score (max probability) */
    call lyfron_extract_threat_score
    
    /* Return as float in %xmm0 */
    cvtsi2ss %eax, %xmm0
    
    popq %r15
    popq %r14
    popq %r13
    popq %r12
    popq %rbx
    popq %rbp
    ret

/* ─── Threat Detection Pipeline ─── */

lyfron_amdxdna_threat_detect:
    /* C ABI: int lyfron_amdxdna_threat_detect(void* data, size_t len, int data_type) */
    /* %rdi = data buffer, %rsi = length, %rdx = type (0=file, 1=memory, 2=network) */
    
    pushq %rbp
    movq %rsp, %rbp
    
    /* Extract features based on data type */
    cmpq $0, %rdx
    je .L_detect_file
    cmpq $1, %rdx
    je .L_detect_memory
    cmpq $2, %rdx
    je .L_detect_network
    
    movq $-1, %rax      /* Invalid type */
    jmp .L_threat_detect_done
    
.L_detect_file:
    call lyfron_extract_file_features
    jmp .L_run_inference
    
.L_detect_memory:
    call lyfron_extract_memory_features
    jmp .L_run_inference
    
.L_detect_network:
    call lyfron_extract_network_features
    
.L_run_inference:
    /* Run through XDNA NPU */
    movq %rax, %rdi     /* Feature vector */
    movq $LYFRON_INPUT_FEATURES, %rsi
    call lyfron_amdxdna_inference
    
    /* %xmm0 now contains threat score 0.0-1.0 */
    /* Convert to confidence integer */
    mulss lyfron_scale_10000(%rip), %xmm0
    cvttss2si %xmm0, %eax
    
    /* Classify threat level */
    cmpl $9500, %eax
    jge .L_threat_critical
    cmpl $8000, %eax
    jge .L_threat_high
    cmpl $5000, %eax
    jge .L_threat_medium
    
    xorq %rax, %rax     /* Low threat - allow */
    jmp .L_threat_detect_done
    
.L_threat_critical:
    movq $4, %rax       /* CRITICAL - block + report + panic */
    jmp .L_threat_detect_done
    
.L_threat_high:
    movq $3, %rax       /* HIGH - block + report */
    jmp .L_threat_detect_done
    
.L_threat_medium:
    movq $2, %rax       /* MEDIUM - flag for review */
    
.L_threat_detect_done:
    popq %rbp
    ret

/* ─── Profile Scoring (Social/Notification Analysis) ─── */

lyfron_amdxdna_profile_score:
    /* C ABI: int lyfron_amdxdna_profile_score(void* profile_data) */
    /* Analyze social profile for trustworthiness */
    
    pushq %rbp
    movq %rsp, %rbp
    
    /* Extract features from profile:
       - Account age
       - Block count
       - Report count
       - Message patterns
       - Link reputation
       - Profile verification status
    */
    
    movq %rdi, %r12
    
    /* Get block count */
    movl LYFRON_PROFILE_BLOCK_COUNT(%r12), %eax
    
    /* If > 10 blocks, flag as suspicious */
    cmpl $10, %eax
    jle .L_profile_check_other
    
    /* High block count - return restricted status */
    movq $LYFRON_PROFILE_RESTRICTED, %rax
    jmp .L_profile_score_done
    
.L_profile_check_other:
    /* Run full profile analysis through NPU */
    call lyfron_extract_profile_features
    movq %rax, %rdi
    movq $128, %rsi         /* Profile feature vector size */
    call lyfron_amdxdna_inference
    
    /* Return trust score */
    mulss lyfron_scale_10000(%rip), %xmm0
    cvttss2si %xmm0, %eax
    
.L_profile_score_done:
    popq %rbp
    ret

/* ─── AIE Security Kernel Microcode ─── */

    .section .rodata
    .align 4096

lyfron_xdna_microcode:
    /* AIE array microcode for security inference */
    /* This is loaded into XDNA NPU's AI Engine tiles */
    
    /* Tile configuration for threat detection pipeline */
    .long 0x00000001    /* Tile[0,0]: Feature extraction */
    .long 0x00000002    /* Tile[1,0]: Convolution layer 1 */
    .long 0x00000003    /* Tile[2,0]: Convolution layer 2 */
    .long 0x00000004    /* Tile[3,0]: Fully connected */
    .long 0x00000005    /* Tile[0,1]: LSTM/attention */
    .long 0x00000006    /* Tile[1,1]: Classification */
    .long 0x00000007    /* Tile[2,1]: Output softmax */
    .long 0x00000008    /* Tile[3,1]: DMA control */
    
    /* Security-specific weights (encrypted, loaded at runtime) */
    .space 32768        /* Weight buffer */
    
lyfron_xdna_microcode_end:

/* ─── Data Section ─── */

    .data
    .align 8

lyfron_license_base:
    .quad 0             /* Set by kernel driver at load time */

lyfron_xdna_input_buf:
    .quad 0             /* DMA input buffer */

lyfron_xdna_output_buf:
    .quad 0             /* DMA output buffer */

lyfron_scale_10000:
    .float 10000.0

lyfron_model_loaded:
    .byte 0

    /* Profile structure offsets */
    .set LYFRON_PROFILE_BLOCK_COUNT,  0x00
    .set LYFRON_PROFILE_REPORT_COUNT, 0x04
    .set LYFRON_PROFILE_AGE_DAYS,    0x08
    .set LYFRON_PROFILE_VERIFIED,    0x0C
    .set LYFRON_PROFILE_RESTRICTED,  0x01
