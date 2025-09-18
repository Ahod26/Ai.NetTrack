import { API_BASE_URL, API_ENDPOINTS } from "./config";

export async function getNewsByDate(dates = null) {
  try {
    let url = `${API_BASE_URL}${API_ENDPOINTS.NEWS.GETNEWSBYDATE}`;

    // Add dates as query parameters if provided
    if (dates && dates.length > 0) {
      const dateParams = dates
        .map((date) => {
          // Convert to YYYY-MM-DD format for better backend parsing
          const dateStr =
            date instanceof Date
              ? date.toISOString().split("T")[0] // Gets just the date part (YYYY-MM-DD)
              : new Date(date).toISOString().split("T")[0];
          return `dates=${encodeURIComponent(dateStr)}`;
        })
        .join("&");
      url += `?${dateParams}`;
    }

    const response = await fetch(url, {
      method: "GET",
      credentials: "include",
    });

    if (!response.ok) {
      throw new Error(`Failed to get news: ${response.status}`);
    }
    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Error getting news:", error);
    throw error;
  }
}
