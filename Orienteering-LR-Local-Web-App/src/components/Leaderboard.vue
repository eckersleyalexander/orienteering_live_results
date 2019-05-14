<template>
    <div class="leaderboard">
        <div v-if="loading">Loading...</div>
        <div v-else>
            <table v-if="data">
                <thead>
                    <tr>
                        <th>Position</th>
                        <th>Name</th>
                        <div v-for="index in legs" :key="index">
                            <div v-if="index !== legs - 1">
                                <th>Leg {{ index + 1 }}</th>
                            </div>
                            <div v-else>
                                <th>Final</th>
                            </div>
                        </div>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="(item, index) in data.list" :key="index">
                        <td>{{ index + 1 }}</td>
                        <td>{{ item.FirstName }} {{ item.Surname }}</td>
                        <div v-for="(item, index) in data.list" :key="index">
                            
                        </div>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
</template>

<script>
export default {
    name: "Leaderboard",
    data() {
        return {
            loading: true,
            data: null,
            legs: 0,
            socket_data: null
        };
    },
    mounted() {
        this.sockets.subscribe("leaderboard", sdata => {
            this.socket_data = sdata.content;
        });
    },
    watch: {
        socket_data: function (val) {
            this.loading = false;

            // calculate max legs
            this.legs = getTotalLegs(val);
            this.data = sortLeaderboard(val);
        }
    }
};

function getTotalLegs(data) {
    var maxTimes = 0;
    for (var i = data.length - 1; i >= 0; i--) {
        maxTimes = Math.max(data[i].length, maxTimes);
    }
    return maxTimes;
}

function sortLeaderboard(data) {

    var wip = data;



    return data;
}

</script>
