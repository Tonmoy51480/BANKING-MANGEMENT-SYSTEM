// SecureBank Core Enterprise Client Scripts

$(document).ready(function () {
    // 1. Auto-dismiss flash alert messages after 6 seconds
    setTimeout(function () {
        $(".alert.alert-dismissible").fadeOut(500, function () {
            $(this).remove();
        });
    }, 6000);

    // 2. Fetch unread notifications count
    if ($("#unreadBadge").length > 0) {
        $.get("/Notification/GetUnreadCount", function (res) {
            if (res && res.count > 0) {
                $("#unreadBadge").text(res.count).show();
            } else {
                $("#unreadBadge").hide();
            }
        });
    }

    // 3. Dynamic Receiver Account Lookup for Transfer Page
    var receiverInput = $("#ReceiverAccountNumber");
    var lookupFeedback = $("#recipientLookupFeedback");

    if (receiverInput.length > 0) {
        var timer = null;
        receiverInput.on("input blur", function () {
            var accNo = $(this).val().trim();
            clearTimeout(timer);

            if (accNo.length < 5) {
                if (lookupFeedback.length) lookupFeedback.html("");
                return;
            }

            timer = setTimeout(function () {
                if (lookupFeedback.length) {
                    lookupFeedback.html('<span class="text-muted small"><i class="fa-solid fa-spinner fa-spin me-1"></i> Validating account...</span>');
                }

                $.get("/Transfer/LookupAccount", { accountNumber: accNo }, function (data) {
                    if (data.found) {
                        lookupFeedback.html('<div class="alert alert-success py-1 px-2 mt-2 mb-0 small"><i class="fa-solid fa-circle-check text-success me-1"></i> <strong>' + data.customerName + '</strong> (' + data.accountType + ' &bull; ' + data.branch + ')</div>');
                    } else {
                        lookupFeedback.html('<div class="alert alert-danger py-1 px-2 mt-2 mb-0 small"><i class="fa-solid fa-triangle-exclamation text-danger me-1"></i> ' + data.message + '</div>');
                    }
                }).fail(function () {
                    if (lookupFeedback.length) {
                        lookupFeedback.html('<span class="text-danger small">Error verifying account.</span>');
                    }
                });
            }, 400);
        });

        // Quick click on beneficiary in transfer view
        $(".select-beneficiary-btn").on("click", function () {
            var acc = $(this).data("account");
            receiverInput.val(acc).trigger("blur");
        });
    }

    // 4. Live EMI Calculator on Loan Application Page
    function updateLoanEmi() {
        var principal = parseFloat($("#PrincipalAmount").val()) || 0;
        var rate = parseFloat($("#InterestRate").val()) || 0;
        var months = parseInt($("#DurationMonths").val()) || 0;

        if (principal > 0 && months > 0) {
            $.get("/Loan/CalculateEmi", { principal: principal, rate: rate, months: months }, function (res) {
                $("#emiMonthlyDisplay").text("$" + res.monthlyPayment.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                $("#emiTotalDisplay").text("$" + res.totalPayment.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                $("#emiInterestDisplay").text("$" + res.totalInterest.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
            });
        }
    }

    $("#PrincipalAmount, #InterestRate, #DurationMonths").on("input change", function () {
        updateLoanEmi();
    });

    if ($("#emiMonthlyDisplay").length > 0) {
        updateLoanEmi();
    }
});

// Print receipt helper
function printReceipt() {
    window.print();
}
