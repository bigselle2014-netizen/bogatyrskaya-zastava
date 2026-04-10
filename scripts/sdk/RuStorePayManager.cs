using System;
using System.Collections.Generic;
using UnityEngine;

namespace BogatyrskayaZastava.Core
{
    // ─────────────────────────────────────────────────────────────────
    // INTERFACE
    // ─────────────────────────────────────────────────────────────────

    public interface IPaymentManager
    {
        void Initialize(Action<bool> onComplete);
        void PurchaseProduct(string productId, Action<PaymentResult> onComplete);
        void RestorePurchases(Action<List<string>> onComplete);
        void GetProducts(List<string> productIds, Action<List<ProductInfo>> onComplete);
        bool IsInitialized { get; }
    }

    // ─────────────────────────────────────────────────────────────────
    // SUPPORTING TYPES
    // ─────────────────────────────────────────────────────────────────

    public enum PaymentResult
    {
        Success,
        Cancelled,
        Failed,
        PendingValidation,
        AlreadyOwned
    }

    [Serializable]
    public class ProductInfo
    {
        public string productId;
        public string title;
        public string description;
        public string priceString;
        public decimal priceAmount;
        public string currency;
        public ProductType type;
    }

    public enum ProductType
    {
        Consumable,     // Рунные камни
        NonConsumable,  // Starter Pack
        Subscription    // Battle Pass
    }

    // ─────────────────────────────────────────────────────────────────
    // STUB IMPLEMENTATION — RuStore Pay SDK v10.1.1
    //
    // TODO: Заменить на реальный RuStore Pay SDK v10.1.1
    // Docs: https://www.rustore.ru/help/sdk/payments
    //
    // ВАЖНО: НЕ использовать старый BillingClient SDK — умирает 01.08.2026
    // Серверная валидация: Cloud Functions → RuStore API → Firestore
    // Регистрация: ServiceLocator.Register<IPaymentManager>(new RuStorePayManager());
    // ─────────────────────────────────────────────────────────────────

    public class RuStorePayManager : IPaymentManager
    {
        private bool _initialized;
        public bool IsInitialized => _initialized;

        public void Initialize(Action<bool> onComplete)
        {
            // TODO: RuStorePayClient.Init(consoleApplicationId, deeplinkScheme);
            // TODO: Check RuStore app availability
            _initialized = true;
            Debug.Log("[RuStorePay STUB] Initialized (Pay SDK v10.1.1).");
            onComplete?.Invoke(true);
        }

        public void PurchaseProduct(string productId, Action<PaymentResult> onComplete)
        {
            if (!_initialized)
            {
                Debug.LogWarning("[RuStorePay STUB] Not initialized.");
                onComplete?.Invoke(PaymentResult.Failed);
                return;
            }

            // TODO: Real flow:
            // 1. RuStorePayClient.PurchaseProduct(productId)
            // 2. On success → get purchase token
            // 3. Send token to Cloud Functions for server-side validation
            // 4. Cloud Functions → RuStore API verify → Firestore write → respond
            // 5. On validated → grant item to player
            // 6. On failed → rollback, show error

            Debug.Log($"[RuStorePay STUB] Purchase: {productId} → Success (stub)");
            onComplete?.Invoke(PaymentResult.Success);
        }

        public void RestorePurchases(Action<List<string>> onComplete)
        {
            if (!_initialized)
            {
                onComplete?.Invoke(new List<string>());
                return;
            }

            // TODO: RuStorePayClient.GetPurchases() → filter confirmed
            Debug.Log("[RuStorePay STUB] RestorePurchases → empty list (stub)");
            onComplete?.Invoke(new List<string>());
        }

        public void GetProducts(List<string> productIds, Action<List<ProductInfo>> onComplete)
        {
            if (!_initialized)
            {
                onComplete?.Invoke(new List<ProductInfo>());
                return;
            }

            // TODO: RuStorePayClient.GetProducts(productIds)
            var stubProducts = new List<ProductInfo>();
            foreach (var id in productIds)
            {
                stubProducts.Add(new ProductInfo
                {
                    productId = id,
                    title = $"[STUB] {id}",
                    description = "Stub product",
                    priceString = "99 ₽",
                    priceAmount = 99m,
                    currency = "RUB",
                    type = ProductType.Consumable
                });
            }

            Debug.Log($"[RuStorePay STUB] GetProducts: {stubProducts.Count} stub items");
            onComplete?.Invoke(stubProducts);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // RECEIPT VALIDATION (client-side DTO for Cloud Functions)
    // ─────────────────────────────────────────────────────────────────

    [Serializable]
    public class ReceiptValidationRequest
    {
        public string userId;
        public string productId;
        public string purchaseToken;
        public string signature;
        // TODO: Send to Cloud Functions endpoint for server-side anti-fraud validation
        // Cloud Functions flow: validate receipt → RuStore API → write to Firestore → return result
    }

    [Serializable]
    public class ReceiptValidationResponse
    {
        public bool isValid;
        public string errorMessage;
        public string transactionId;
    }
}
