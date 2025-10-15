using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Stripe;

namespace stripe_custom_pay.Controllers
{
    public class PaymentIntentApiController : Controller
    {
        #region Charge 사용하여 결제
        [HttpGet]
        public IActionResult CreateTokenAndCharge()
        {
            try
            {
                // 카드 정보를 사용하여 토큰 생성
                var tokenOptions = new TokenCreateOptions
                {
                    Card = new TokenCardOptions
                    {
                        Number = "4242424242424242",  // 테스트 카드 번호
                        ExpMonth = "5",
                        ExpYear = "2026",
                        Cvc = "314",
                    },
                };

                var tokenService = new TokenService();
                Token stripeToken = tokenService.Create(tokenOptions);

                // 생성된 토큰을 사용하여 결제 처리
                var chargeOptions = new ChargeCreateOptions
                {
                    Amount = 5000, // Amount in cents (e.g., $50.00)
                    Currency = "usd",
                    Description = "Example charge using token",
                    Source = stripeToken.Id, // 생성된 토큰 사용
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
        /// 카드 정보를 사용하여 토큰 생성
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult CreateToken([FromBody] CreateTokenRequest request)
        {
            try
            {
                // 1. 카드 정보를 사용하여 토큰 생성
                var tokenOptions = new TokenCreateOptions
                {
                    Card = new TokenCardOptions
                    {
                        Number = request.CardNumber,  // 카드 번호
                        ExpMonth = request.ExpMonth,  // 만료 월
                        ExpYear = "20" + request.ExpYear,    // 만료 연도
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
        /// 생성된 토큰을 사용하여 결제 처리
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ProcessCharge([FromBody] ChargeRequest request)
        {
            try
            {
                // 2. 생성된 토큰을 사용하여 결제 처리
                var chargeOptions = new ChargeCreateOptions
                {
                    Amount = request.Amount,            // 금액 (센트 단위)
                    Currency = request.Currency,        // 통화 코드 (예: "usd")
                    Description = request.Description,  // 결제 설명
                    Source = request.Token,             // 앞에서 생성된 토큰 사용
                    ReceiptEmail = request.ReceiptEmail,// 이메일                    
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
        /// 결제 환불 처리
        /// </summary>
        /// <param name="chargeId">환불할 결제의 Charge ID</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ChargeRefund([FromBody] RefundRequest request)
        {
            try
            {
                // 1. 환불 옵션 설정
                var refundOptions = new RefundCreateOptions
                {
                    Charge = request.ChargeId, // 환불할 결제의 Charge ID
                    Amount = request.Amount, // 환불 금액 (선택 사항, 설정하지 않으면 전체 금액 환불)
                };

                // 2. RefundService를 사용하여 환불 처리
                var refundService = new RefundService();
                Refund refund = refundService.Create(refundOptions);

                return Ok(refund); // 환불 성공 시 환불 정보 반환
            }
            catch (StripeException ex)
            {
                // StripeException 발생 시 에러 메시지 반환
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
            public long Amount { get; set; } // 금액 (센트 단위)
            public string Currency { get; set; } // 통화 코드
            public string ReceiptEmail { get; set; } // 이메일
            public string CardNumber { get; set; } // 카드번호
            public string Description { get; set; } // 결제 설명
            public Dictionary<string, string> Metadata { get; set; } // Metadata

        }

        public class RefundRequest
        {
            public string ChargeId { get; set; } // 환불할 결제의 Charge ID
            public long? Amount { get; set; } // 환불 금액 (선택 사항, 단위: 센트)
        }

        #endregion

        #region [샘플소스]

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
