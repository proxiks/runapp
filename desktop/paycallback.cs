#if ANDROID
using Android.App;
using Android.Content;
using Com.Razorpay;

namespace RunApp.Mobile.Platforms.Android;

[Activity(Exported = true)]
public class RazorpayPaymentCallback : Activity, IPaymentResultWithDataListener
{
    private static Action<PaymentResultData>? _callback;

    public static void RegisterCallback(Action<PaymentResultData> callback)
    {
        _callback = callback;
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Handle payment result from intent
        var paymentId = Intent?.GetStringExtra("razorpay_payment_id");
        var signature = Intent?.GetStringExtra("razorpay_signature");
        var error = Intent?.GetStringExtra("error");

        if (!string.IsNullOrEmpty(paymentId))
        {
            _callback?.Invoke(new PaymentResultData 
            { 
                Success = true, 
                PaymentId = paymentId, 
                Signature = signature 
            });
        }
        else
        {
            _callback?.Invoke(new PaymentResultData 
            { 
                Success = false, 
                Error = error ?? "Payment failed" 
            });
        }

        Finish();
    }

    public void OnPaymentSuccess(string razorpayPaymentId, PaymentData paymentData)
    {
        _callback?.Invoke(new PaymentResultData
        {
            Success = true,
            PaymentId = razorpayPaymentId,
            Signature = paymentData.GetSignature()
        });
        Finish();
    }

    public void OnPaymentError(int code, string description, PaymentData paymentData)
    {
        _callback?.Invoke(new PaymentResultData
        {
            Success = false,
            Error = description,
            Code = code
        });
        Finish();
    }
}

public class PaymentResultData
{
    public bool Success { get; set; }
    public string? PaymentId { get; set; }
    public string? Signature { get; set; }
    public string? Error { get; set; }
    public int Code { get; set; }
}
#endif