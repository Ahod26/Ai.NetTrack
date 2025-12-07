import { API_BASE_URL, API_ENDPOINTS } from "./config";

export async function getNewsByDate(dates = null, newsType = null) {
  try {
    let url = `${API_BASE_URL}${API_ENDPOINTS.NEWS.GET_NEWS_BY_DATE}`;

    // Add news type to route if provided
    if (newsType && newsType > 0) {
      url = `${API_BASE_URL}${API_ENDPOINTS.NEWS.GET_NEWS_BY_DATE}/${newsType}`;
    }

    // Add dates as query parameters if provided
    if (dates && dates.length > 0) {
      const dateParams = dates
        .map((date) => {
          // Convert to YYYY-MM-DD format using local timezone (not UTC)
          const d = date instanceof Date ? date : new Date(date);
          const year = d.getFullYear();
          const month = String(d.getMonth() + 1).padStart(2, "0");
          const day = String(d.getDate()).padStart(2, "0");
          const dateStr = `${year}-${month}-${day}`;
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

export async function getNewsBySearch(searchTerm) {
  try {
    const url = `${API_BASE_URL}${
      API_ENDPOINTS.NEWS.GET_NEWS_BY_SEARCH
    }?term=${encodeURIComponent(searchTerm)}`;

    const response = await fetch(url, {
      method: "GET",
      credentials: "include",
    });

    if (!response.ok) {
      throw new Error(`Failed to search news: ${response.status}`);
    }
    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Error searching news:", error);
    throw error;
  }
}
