using System.Net;
using CyberSecOS.Core.Api;
using CyberSecOS.Core.Models;
using CyberSecOS.Core.Tests.Fakes;

namespace CyberSecOS.Core.Tests;

public class ApiClientTests
{
    private const string TokenJson = """
        {"access_token":"abc","token_type":"bearer","expires_in":3600,
         "user":{"id":1,"username":"adam","display_name":"Adam","target_role":"SOC Analyst",
                 "weekly_goal_hours":10,"created_at":"2026-10-19T22:00:00","last_login_at":null}}
        """;

    [Fact]
    public async Task LoginSendsSnakeCaseJsonAndParsesToken()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, TokenJson);
        var api = new CyberSecApiClient(handler.CreateClient());

        var token = await api.LoginAsync(new LoginRequest("adam", "secret"));

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/auth/login", request.RequestUri!.AbsolutePath);
        Assert.Equal("""{"username":"adam","password":"secret"}""", body);
        Assert.Equal("abc", token.AccessToken);
        Assert.Equal("Adam", token.User.DisplayName);
        Assert.Equal(10, token.User.WeeklyGoalHours);
    }

    [Fact]
    public async Task SendsBearerTokenWhenSet()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """{"registered":true}""");
        var api = new CyberSecApiClient(handler.CreateClient()) { AccessToken = "tok" };

        await api.GetAuthStatusAsync();

        var auth = handler.Requests[0].Request.Headers.Authorization;
        Assert.Equal("Bearer", auth!.Scheme);
        Assert.Equal("tok", auth.Parameter);
    }

    [Fact]
    public async Task ProfileUpdateOmitsUnsetFields()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """
            {"id":1,"username":"adam","display_name":"A","target_role":"SOC Analyst","weekly_goal_hours":12,"created_at":"2026-10-19T22:00:00","last_login_at":null}
            """);
        var api = new CyberSecApiClient(handler.CreateClient());

        await api.UpdateProfileAsync(new ProfileUpdate(WeeklyGoalHours: 12));

        Assert.Equal("""{"weekly_goal_hours":12}""", handler.Requests[0].Body);
    }

    [Fact]
    public async Task StringDetailBecomesExceptionMessage()
    {
        var handler = new StubHttpHandler(HttpStatusCode.Unauthorized, """{"detail":"Invalid username or password"}""");
        var api = new CyberSecApiClient(handler.CreateClient());

        var ex = await Assert.ThrowsAsync<ApiException>(() => api.LoginAsync(new LoginRequest("a", "b")));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.True(ex.IsUnauthorized);
        Assert.Equal("Invalid username or password", ex.Message);
    }

    [Fact]
    public async Task ValidationDetailUsesFirstMessage()
    {
        var handler = new StubHttpHandler((HttpStatusCode)422, """{"detail":[{"msg":"String should have at least 3 characters"}]}""");
        var api = new CyberSecApiClient(handler.CreateClient());

        var ex = await Assert.ThrowsAsync<ApiException>(() => api.RegisterAsync(new RegisterRequest("a", "b", "c")));

        Assert.Equal("String should have at least 3 characters", ex.Message);
    }

    [Fact]
    public async Task NoContentResponseSucceeds()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var api = new CyberSecApiClient(handler.CreateClient());

        await api.ChangePasswordAsync(new PasswordChange("old", "new"));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task UnreachableServerGivesFriendlyError()
    {
        var handler = new StubHttpHandler(_ => throw new HttpRequestException("connection refused"));
        var api = new CyberSecApiClient(handler.CreateClient());

        var ex = await Assert.ThrowsAsync<ApiException>(() => api.GetAuthStatusAsync());

        Assert.Null(ex.StatusCode);
        Assert.Contains("Cannot reach", ex.Message);
    }

    [Fact]
    public async Task MalformedResponseBecomesApiException()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """{"registered": "not-a-bool"}""");
        var api = new CyberSecApiClient(handler.CreateClient());

        var ex = await Assert.ThrowsAsync<ApiException>(() => api.GetAuthStatusAsync());

        Assert.Contains("does not understand", ex.Message);
    }

    [Fact]
    public async Task AnalyticsRequestsWeeksAndMapsSnakeCaseKeys()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """
            {"study_trend":[{"week_start":"2026-10-19","minutes":180,"active_days":3,"rolling_average_minutes":100}],
             "study_patterns":{"by_weekday":[{"weekday":0,"label":"Mon","minutes":60}],"by_part_of_day":[{"part":"Evening","minutes":180}],"peak_part_of_day":"Evening"},
             "velocity":{"total_lessons":72,"completed_lessons":8,"remaining_lessons":64,"completed_last_4_weeks":4,"lessons_per_week":1.0,"projected_weeks_to_finish":64,"projected_finish_date":"2028-01-10"},
             "quiz_performance":{"total_attempts":6,"average_score":72,"score_trend":15,"weakest_domains":[{"domain":"Linux Security","average_score":55,"lessons_attempted":2}],"weakest_objectives":[{"id":"secplus:4.9","title":"Incident response","certification":"CompTIA Security+","average_score":50,"lessons_attempted":1}]}}
            """);
        var api = new CyberSecApiClient(handler.CreateClient());

        var analytics = await api.GetAnalyticsAsync();

        Assert.Contains("api/analytics?weeks=8", handler.Requests[0].Request.RequestUri!.ToString());
        Assert.Contains("tz_offset_minutes=", handler.Requests[0].Request.RequestUri!.ToString());
        // The digit-bearing key round-trips only with the explicit JsonPropertyName.
        Assert.Equal(4, analytics.Velocity.CompletedLast4Weeks);
        Assert.Equal(new DateOnly(2028, 1, 10), analytics.Velocity.ProjectedFinishDate);
        Assert.Equal("Evening", analytics.StudyPatterns.PeakPartOfDay);
    }
}
