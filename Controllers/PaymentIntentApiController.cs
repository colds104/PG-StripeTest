using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Stripe;

namespace stripe_custom_pay.Controllers
{
    public class PaymentIntentApiController : Controller
    {
        #region Charge ����Ͽ� ����
        [HttpGet]
        public IActionResult CreateTokenAndCharge()
        {
            try
            {
                // ī�� ������ ����Ͽ� ��ū ����
                var tokenOptions = new TokenCreateOptions
                {
                    Card = new TokenCardOptions
                    {
                        Number = "4242424242424242",  // �׽�Ʈ ī�� ��ȣ
                        ExpMonth = "5",
                        ExpYear = "2026",
                        Cvc = "314",
                    },
                };

                var tokenService = new TokenService();
                Token stripeToken = tokenService.Create(tokenOptions);

                // ������ ��ū�� ����Ͽ� ���� ó��
                var chargeOptions = new ChargeCreateOptions
                {
                    Amount = 5000, // Amount in cents (e.g., $50.00)
                    Currency = "usd",
                    Description = "Example charge using token",
                    Source = stripeToken.Id, // ������ ��ū ���
                };

                var chargeService = new ChargeService();
                Charge charge = chargeService.Create(chargeOptions);

                return Ok(charge);
            }
            catch (StripeException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        /// <summary>
        /// ī�� ������ ����Ͽ� ��ū ����
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult CreateToken([FromBody] CreateTokenRequest request)
        {
            try
            {
                // 1. ī�� ������ ����Ͽ� ��ū ����
                var tokenOptions = new TokenCreateOptions
                {
                    Card = new TokenCardOptions
                    {
                        Number = request.CardNumber,  // ī�� ��ȣ
                        ExpMonth = request.ExpMonth,  // ���� ��
                        ExpYear = "20" + request.ExpYear,    // ���� ����
                        Cvc = request.Cvc,            // CVC
                    },
                };

                var tokenService = new TokenService();
                Token stripeToken = tokenService.Create(tokenOptions);

                return Ok(new { token = stripeToken.Id });
            }
            catch (StripeException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// ������ ��ū�� ����Ͽ� ���� ó��
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ProcessCharge([FromBody] ChargeRequest request)
        {
            try
            {
                // 2. ������ ��ū�� ����Ͽ� ���� ó��
                var chargeOptions = new ChargeCreateOptions
                {
                    Amount = request.Amount,            // �ݾ� (��Ʈ ����)
                    Currency = request.Currency,        // ��ȭ �ڵ� (��: "usd")
                    Description = request.Description,  // ���� ����
                    Source = request.Token,             // �տ��� ������ ��ū ���
                    ReceiptEmail = request.ReceiptEmail,// �̸���                    
                    Metadata = request.Metadata         // Metadata                
                };

                var chargeService = new ChargeService();
                Charge charge = chargeService.Create(chargeOptions);

                return Ok(charge);
            }
            catch (StripeException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// ���� ȯ�� ó��
        /// </summary>
        /// <param name="chargeId">ȯ���� ������ Charge ID</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ChargeRefund([FromBody] RefundRequest request)
        {
            try
            {
                // 1. ȯ�� �ɼ� ����
                var refundOptions = new RefundCreateOptions
                {
                    Charge = request.ChargeId, // ȯ���� ������ Charge ID
                    Amount = request.Amount, // ȯ�� �ݾ� (���� ����, �������� ������ ��ü �ݾ� ȯ��)
                };

                // 2. RefundService�� ����Ͽ� ȯ�� ó��
                var refundService = new RefundService();
                Refund refund = refundService.Create(refundOptions);

                return Ok(refund); // ȯ�� ���� �� ȯ�� ���� ��ȯ
            }
            catch (StripeException ex)
            {
                // StripeException �߻� �� ���� �޽��� ��ȯ
                return BadRequest(new { error = ex.Message });
            }
        }


        public class CreateTokenRequest
        {
            public string CardNumber { get; set; }
            public string ExpMonth { get; set; }
            public string ExpYear { get; set; }
            public string Cvc { get; set; }
        }

        public class ChargeRequest
        {
            public string Token { get; set; }
            public long Amount { get; set; } // �ݾ� (��Ʈ ����)
            public string Currency { get; set; } // ��ȭ �ڵ�
            public string ReceiptEmail { get; set; } // �̸���
            public string CardNumber { get; set; } // ī���ȣ
            public string Description { get; set; } // ���� ����
            public Dictionary<string, string> Metadata { get; set; } // Metadata

        }

        public class RefundRequest
        {
            public string ChargeId { get; set; } // ȯ���� ������ Charge ID
            public long? Amount { get; set; } // ȯ�� �ݾ� (���� ����, ����: ��Ʈ)
        }

        #endregion

        #region [���üҽ�]

        [HttpPost]
        public ActionResult Create([FromBody] PaymentIntentCreateRequest request)
        {
            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = paymentIntentService.Create(new PaymentIntentCreateOptions
            {
                Amount = CalculateOrderAmount(request.Items),
                Currency = "usd",
                PaymentMethod = "pm_card_visa",
                // In the latest version of the API, specifying the `automatic_payment_methods` parameter is optional because Stripe enables its functionality by default.
                //AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                //{
                //    Enabled = true,
                //},
            });

            return Json(new { clientSecret = paymentIntent.ClientSecret });
        }

        private int CalculateOrderAmount(Item[] items)
        {
            // Calculate the order total on the server to prevent
            // people from directly manipulating the amount on the client
            int total = 0;
            foreach (Item item in items)
            {
                total += Convert.ToInt32(item.Amount);
            }
            return total;
        }

        public class Item
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("amount")]
            public string Amount { get; set; }
        }

        public class PaymentIntentCreateRequest
        {
            [JsonProperty("items")]
            public Item[] Items { get; set; }
        }

        #endregion

    }
}
